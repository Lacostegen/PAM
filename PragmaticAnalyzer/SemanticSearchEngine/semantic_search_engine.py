# -*- coding: utf-8 -*-
from __future__ import annotations

import argparse
import json
import math
import re
import sys
from pathlib import Path
from typing import Any

import numpy as np

try:
    from pymorphy3 import MorphAnalyzer
    _MORPH = MorphAnalyzer()
except Exception:
    _MORPH = None

TOKEN_RE = re.compile(r"[A-Za-zА-Яа-яЁё0-9]+(?:[._:-][A-Za-zА-Яа-яЁё0-9]+)*")
BDU_RE = re.compile(r"\bBDU[:\-\s]?\d{4}[-:]\d{4,}\b", re.IGNORECASE)
CVE_RE = re.compile(r"\bCVE-\d{4}-\d{4,}\b", re.IGNORECASE)
CVSS_CIA_RE = re.compile(r"(?:^|/)([CIA]):([HLNPC])(?=/|$)", re.IGNORECASE)

STOPWORDS = {
    "и", "в", "во", "на", "с", "со", "по", "для", "от", "до", "из", "у", "к", "о", "об", "при",
    "что", "как", "это", "или", "а", "но", "после", "часть", "данные", "файл", "файлы", "компьютер",
}


def normalize_text(text: Any) -> str:
    return str(text or "").lower().replace("_x000d_", " ").replace("\r", " ").replace("\n", " ")


def tokenize(text: Any) -> list[str]:
    raw = TOKEN_RE.findall(normalize_text(text))
    result: list[str] = []
    for token in raw:
        if _MORPH is not None and re.search(r"[а-яё]", token):
            try:
                token = _MORPH.parse(token)[0].normal_form
            except Exception:
                pass
        if len(token) > 1 and token not in STOPWORDS:
            result.append(token)
    return result


def join_fields(item: dict[str, Any], fields: list[str]) -> str:
    return " ".join(str(item.get(field) or "") for field in fields)


def parse_truth(value: Any) -> int:
    text = normalize_text(value)
    if text in {"1", "true", "yes", "да", "истина"}:
        return 1
    if "1" in text or "да" in text or "наруш" in text:
        return 1
    return 0


def parse_cvss_cia(cvss: Any) -> dict[str, str]:
    result = {"C": "N", "I": "N", "A": "N"}
    text = str(cvss or "").upper().replace(" ", "")
    if not text or "/" not in text:
        return result
    for match in CVSS_CIA_RE.finditer(text):
        metric = match.group(1).upper()
        value = match.group(2).upper()
        if metric in result:
            result[metric] = value
    return result


def required_cia(vulnerability: dict[str, Any]) -> tuple[int, int, int, str]:
    cvss3 = vulnerability.get("cvss3")
    cvss2 = vulnerability.get("cvss2")
    cia = parse_cvss_cia(cvss3)
    source = str(cvss3 or "")
    if cia == {"C": "N", "I": "N", "A": "N"}:
        cia = parse_cvss_cia(cvss2)
        source = str(cvss2 or "")
    return (
        1 if cia["C"] != "N" else 0,
        1 if cia["I"] != "N" else 0,
        1 if cia["A"] != "N" else 0,
        source,
    )


def doc_vector(tokens: list[str], word_index: dict[str, int], weights: np.ndarray, idf: dict[str, float] | None = None) -> np.ndarray:
    dim = weights.shape[1]
    vec = np.zeros((dim,), dtype=np.float32)
    total = 0.0
    for token in tokens:
        idx = word_index.get(token)
        if idx is None:
            continue
        weight = float(idf.get(token, 1.0)) if idf else 1.0
        vec += weights[idx] * weight
        total += weight
    if total > 0:
        vec /= total
    norm = float(np.linalg.norm(vec))
    if norm > 0:
        vec /= norm
    return vec


def cosine_scores(query_vec: np.ndarray, matrix: np.ndarray) -> np.ndarray:
    if float(np.linalg.norm(query_vec)) <= 0:
        return np.zeros((matrix.shape[0],), dtype=np.float32)
    return matrix @ query_vec


def build_idf(docs: list[list[str]]) -> dict[str, float]:
    total = max(len(docs), 1)
    df: dict[str, int] = {}
    for doc in docs:
        for token in set(doc):
            df[token] = df.get(token, 0) + 1
    return {token: math.log((1 + total) / (1 + count)) + 1.0 for token, count in df.items()}


def exact_score(query: str, query_tokens: set[str], item: dict[str, Any], doc_tokens: set[str]) -> tuple[float, list[str]]:
    score = 0.0
    reasons: list[str] = []
    item_text = normalize_text(join_fields(item, list(item.keys())))

    for bdu in BDU_RE.findall(query):
        if normalize_text(bdu).replace(" ", "").replace("-", ":") in item_text.replace("-", ":"):
            score += 0.55
            reasons.append(f"точное совпадение {bdu.upper()}")

    for cve in CVE_RE.findall(query):
        if normalize_text(cve) in item_text:
            score += 0.45
            reasons.append(f"совпадение {cve.upper()}")

    overlap = sorted(query_tokens & doc_tokens)
    important = [token for token in overlap if len(token) >= 4][:8]
    if important:
        score += min(0.45, 0.08 * len(important))
        reasons.append("ключевые слова: " + ", ".join(important[:5]))

    return min(score, 0.75), reasons


def confidence(score: float) -> str:
    if score >= 0.70:
        return "высокая"
    if score >= 0.45:
        return "средняя"
    return "низкая"


def load_model(engine_dir: Path) -> tuple[np.ndarray, dict[str, int]]:
    model_path = engine_dir / "glove_model.npz"
    if not model_path.exists():
        raise FileNotFoundError(f"Не найдена GloVe-модель: {model_path}")
    data = np.load(model_path, allow_pickle=True)
    return data["W"].astype(np.float32), data["word_index"].item()


def run(request: dict[str, Any], engine_dir: Path) -> dict[str, Any]:
    query = str(request.get("query") or "").strip()
    vulnerabilities = list(request.get("vulnerabilities") or [])
    threats = list(request.get("threats") or [])
    use_cvss = bool(request.get("useCvss", True))
    top_v = int(request.get("topVulnerabilities", 7))
    top_t = int(request.get("topThreatsPerVulnerability", 2))
    min_score = float(request.get("minScore", 0.18))

    query_tokens = tokenize(query)
    if len(query_tokens) < 2 and not BDU_RE.search(query) and not CVE_RE.search(query):
        return {
            "status": "empty",
            "message": "Недостаточно данных для надежного поиска. Уточните описание инцидента.",
            "matches": [],
        }

    weights, word_index = load_model(engine_dir)

    vuln_fields = [
        "id", "name", "description", "vendor", "software", "version", "type", "os", "class",
        "dangerLevel", "measure", "exploit", "otherIdentifier", "incident", "operatingMethod", "cwe",
    ]
    threat_fields = ["id", "name", "description", "source", "objectInfluence"]

    vuln_tokens = [tokenize(join_fields(v, vuln_fields)) for v in vulnerabilities]
    threat_tokens = [tokenize(join_fields(t, threat_fields)) for t in threats]
    idf = build_idf(vuln_tokens + threat_tokens + [query_tokens])

    query_vec = doc_vector(query_tokens, word_index, weights, idf)
    if float(np.linalg.norm(query_vec)) <= 0 and not BDU_RE.search(query) and not CVE_RE.search(query):
        return {
            "status": "empty",
            "message": "В запросе не найдено терминов, известных поисковой модели. Уточните описание инцидента.",
            "matches": [],
        }

    vuln_matrix = np.vstack([doc_vector(tokens, word_index, weights, idf) for tokens in vuln_tokens])
    threat_matrix = np.vstack([doc_vector(tokens, word_index, weights, idf) for tokens in threat_tokens])
    semantic = cosine_scores(query_vec, vuln_matrix)

    ranked_vulns: list[tuple[int, float, list[str]]] = []
    query_token_set = set(query_tokens)
    for idx, vuln in enumerate(vulnerabilities):
        exact, reasons = exact_score(query, query_token_set, vuln, set(vuln_tokens[idx]))
        combined = max(float(semantic[idx]), 0.0) * 0.72 + exact
        if combined >= min_score:
            ranked_vulns.append((idx, min(combined, 1.0), reasons))

    ranked_vulns.sort(key=lambda item: item[1], reverse=True)
    ranked_vulns = ranked_vulns[:top_v]

    matches: list[dict[str, Any]] = []
    used_pairs: set[tuple[str, str]] = set()

    for vuln_idx, vuln_score, vuln_reasons in ranked_vulns:
        vuln = vulnerabilities[vuln_idx]
        need_c, need_i, need_a, cvss_source = required_cia(vuln)
        candidates: list[int] = []
        for threat_idx, threat in enumerate(threats):
            if use_cvss:
                if need_c and not parse_truth(threat.get("confidentiality")):
                    continue
                if need_i and not parse_truth(threat.get("integrity")):
                    continue
                if need_a and not parse_truth(threat.get("availability")):
                    continue
            candidates.append(threat_idx)

        if not candidates and use_cvss:
            matches.append({
                "coefficient": round(vuln_score, 3),
                "confidence": confidence(vuln_score),
                "reason": "Найдена уязвимость, но угрозы по CVSS-фильтру не найдены. " + "; ".join(vuln_reasons),
                "vulnerabilityGuid": vuln.get("guid"),
                "threatGuid": None,
                "vulnerabilityScore": round(vuln_score, 3),
                "threatScore": 0.0,
            })
            continue

        base_vec = vuln_matrix[vuln_idx]
        threat_scores = [(idx, float(threat_matrix[idx] @ base_vec)) for idx in candidates]
        threat_scores.sort(key=lambda item: item[1], reverse=True)

        for threat_idx, threat_score in threat_scores[:top_t]:
            pair = (str(vuln.get("guid")), str(threats[threat_idx].get("guid")))
            if pair in used_pairs:
                continue
            used_pairs.add(pair)

            combined = min(1.0, vuln_score * 0.78 + max(threat_score, 0.0) * 0.22)
            reasons = []
            if vuln_reasons:
                reasons.append("; ".join(vuln_reasons))
            if use_cvss and cvss_source:
                reasons.append(f"CVSS указывает воздействия C={need_c}, I={need_i}, A={need_a}")
            reasons.append("угроза подобрана по близости к описанию уязвимости")

            matches.append({
                "coefficient": round(combined, 3),
                "confidence": confidence(combined),
                "reason": ". ".join(reasons),
                "vulnerabilityGuid": vuln.get("guid"),
                "threatGuid": threats[threat_idx].get("guid"),
                "vulnerabilityScore": round(vuln_score, 3),
                "threatScore": round(threat_score, 3),
            })

    matches.sort(key=lambda item: item["coefficient"], reverse=True)
    if not matches:
        return {
            "status": "empty",
            "message": "Надежных совпадений не найдено. Уточните описание инцидента или отключите CVSS-фильтр.",
            "matches": [],
        }

    return {
        "status": "ok",
        "message": f"Найдено вариантов: {len(matches)}",
        "matches": matches[:10],
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--request", required=True)
    parser.add_argument("--response", required=True)
    args = parser.parse_args()

    try:
        request_path = Path(args.request)
        response_path = Path(args.response)
        request = json.loads(request_path.read_text(encoding="utf-8"))
        response = run(request, Path(__file__).resolve().parent)
        response_path.write_text(json.dumps(response, ensure_ascii=False, indent=2), encoding="utf-8")
        return 0
    except Exception as exc:
        error = {"status": "error", "message": str(exc), "matches": []}
        try:
            Path(args.response).write_text(json.dumps(error, ensure_ascii=False, indent=2), encoding="utf-8")
        except Exception:
            pass
        print(str(exc), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
