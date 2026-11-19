param(
    [string]$ExecutablePath = (Join-Path $PSScriptRoot '..\bin\Debug\net8.0-windows7.0\PragmaticAnalyzer.exe'),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'manual_screenshots')
)

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

Add-Type @'
using System;
using System.Runtime.InteropServices;

public static class NativeWindowCapture
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int X,
        int Y,
        int cx,
        int cy,
        uint uFlags);
}
'@

function Wait-Until {
    param(
        [scriptblock]$Condition,
        [int]$TimeoutSeconds = 30,
        [int]$PollMilliseconds = 250
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        $result = & $Condition
        if ($result) {
            return $result
        }

        Start-Sleep -Milliseconds $PollMilliseconds
    } while ([DateTime]::UtcNow -lt $deadline)

    throw "Timed out after $TimeoutSeconds seconds."
}

function Get-MainAutomationElement {
    param([System.Diagnostics.Process]$Process)

    $handle = Wait-Until -TimeoutSeconds 40 -Condition {
        $Process.Refresh()
        if ($Process.HasExited) {
            throw "PragmaticAnalyzer exited before its main window appeared."
        }

        if ($Process.MainWindowHandle -ne [IntPtr]::Zero) {
            return $Process.MainWindowHandle
        }

        return $null
    }

    return [System.Windows.Automation.AutomationElement]::FromHandle($handle)
}

function Find-ElementByName {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Name,
        [System.Windows.Automation.ControlType]$ControlType,
        [int]$TimeoutSeconds = 12
    )

    return Wait-Until -TimeoutSeconds $TimeoutSeconds -Condition {
        $nameCondition = [System.Windows.Automation.PropertyCondition]::new(
            [System.Windows.Automation.AutomationElement]::NameProperty,
            $Name)
        $typeCondition = [System.Windows.Automation.PropertyCondition]::new(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            $ControlType)
        $condition = [System.Windows.Automation.AndCondition]::new($nameCondition, $typeCondition)
        $element = $Root.FindFirst(
            [System.Windows.Automation.TreeScope]::Descendants,
            $condition)

        if ($element) {
            return $element
        }

        return $null
    }
}

function Invoke-NamedButton {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Name,
        [int]$DelayMilliseconds = 1300
    )

    $button = Find-ElementByName -Root $Root -Name $Name -ControlType ([System.Windows.Automation.ControlType]::Button)
    $pattern = $button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    $pattern.Invoke()
    Start-Sleep -Milliseconds $DelayMilliseconds
}

function Toggle-NamedButton {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Name,
        [int]$DelayMilliseconds = 900
    )

    $button = Find-ElementByName -Root $Root -Name $Name -ControlType ([System.Windows.Automation.ControlType]::Button)
    $pattern = $button.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
    $pattern.Toggle()
    Start-Sleep -Milliseconds $DelayMilliseconds
}

function Select-ComboBoxItem {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$ItemName
    )

    $comboCondition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::ComboBox)
    $comboBoxes = $Root.FindAll(
        [System.Windows.Automation.TreeScope]::Descendants,
        $comboCondition)

    foreach ($comboBox in $comboBoxes) {
        try {
            $expandPattern = $comboBox.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern)
            $expandPattern.Expand()
            Start-Sleep -Milliseconds 350

            $item = Find-ElementByName -Root ([System.Windows.Automation.AutomationElement]::RootElement) -Name $ItemName -ControlType ([System.Windows.Automation.ControlType]::ListItem) -TimeoutSeconds 1
            if ($item) {
                $selectionPattern = $item.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
                $selectionPattern.Select()
                $expandPattern.Collapse()
                Start-Sleep -Milliseconds 1000
                return
            }

            $expandPattern.Collapse()
        }
        catch {
            try {
                $expandPattern.Collapse()
            }
            catch {
            }
        }
    }

    throw "Could not find combo-box item '$ItemName'."
}

function Select-SecondComboBoxItemByCurrentValue {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$CurrentValue
    )

    $comboCondition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::ComboBox)
    $comboBoxes = $Root.FindAll(
        [System.Windows.Automation.TreeScope]::Descendants,
        $comboCondition)

    foreach ($comboBox in $comboBoxes) {
        try {
            $selectionPattern = $comboBox.GetCurrentPattern([System.Windows.Automation.SelectionPattern]::Pattern)
            $selectedItems = $selectionPattern.Current.GetSelection()
            if ($selectedItems.Count -eq 0 -or $selectedItems[0].Current.Name -ne $CurrentValue) {
                continue
            }

            $comboBox.SetFocus()
            Start-Sleep -Milliseconds 250
            [System.Windows.Forms.SendKeys]::SendWait('{HOME}{DOWN}{ENTER}')
            Start-Sleep -Milliseconds 1000
            return
        }
        catch {
        }
    }

    throw "Could not find a combo box with current value '$CurrentValue'."
}

function Scroll-SettingsToBottom {
    param([System.Windows.Automation.AutomationElement]$Root)

    $scrollCondition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::IsScrollPatternAvailableProperty,
        $true)
    $scrollElements = $Root.FindAll(
        [System.Windows.Automation.TreeScope]::Descendants,
        $scrollCondition)

    foreach ($element in $scrollElements) {
        try {
            $scrollPattern = $element.GetCurrentPattern([System.Windows.Automation.ScrollPattern]::Pattern)
            if ($scrollPattern.Current.VerticallyScrollable) {
                $scrollPattern.SetScrollPercent(
                    [System.Windows.Automation.ScrollPattern]::NoScroll,
                    100)
            }
        }
        catch {
        }
    }

    Start-Sleep -Milliseconds 900
}

function Save-WindowScreenshot {
    param(
        [IntPtr]$WindowHandle,
        [string]$FileName
    )

    [NativeWindowCapture]::ShowWindow($WindowHandle, 9) | Out-Null
    [NativeWindowCapture]::SetForegroundWindow($WindowHandle) | Out-Null
    Start-Sleep -Milliseconds 350

    $rect = [NativeWindowCapture+RECT]::new()
    if (-not [NativeWindowCapture]::GetWindowRect($WindowHandle, [ref]$rect)) {
        throw "Could not read the application window rectangle."
    }

    $width = $rect.Right - $rect.Left
    $height = $rect.Bottom - $rect.Top
    $bitmap = [System.Drawing.Bitmap]::new($width, $height)
    try {
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        try {
            $graphics.CopyFromScreen(
                $rect.Left,
                $rect.Top,
                0,
                0,
                [System.Drawing.Size]::new($width, $height),
                [System.Drawing.CopyPixelOperation]::SourceCopy)
        }
        finally {
            $graphics.Dispose()
        }

        $target = Join-Path $OutputDirectory $FileName
        $bitmap.Save($target, [System.Drawing.Imaging.ImageFormat]::Png)
        Write-Output $target
    }
    finally {
        $bitmap.Dispose()
    }
}

function Dismiss-MessageBoxes {
    param([int]$ProcessId)

    $processCondition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty,
        $ProcessId)
    $windows = [System.Windows.Automation.AutomationElement]::RootElement.FindAll(
        [System.Windows.Automation.TreeScope]::Children,
        $processCondition)

    foreach ($window in $windows) {
        try {
            $ok = Find-ElementByName -Root $window -Name 'OK' -ControlType ([System.Windows.Automation.ControlType]::Button) -TimeoutSeconds 1
            if ($ok) {
                $invoke = $ok.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
                $invoke.Invoke()
            }
        }
        catch {
        }
    }
}

$resolvedExecutable = (Resolve-Path -LiteralPath $ExecutablePath).Path
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$startInfo = [System.Diagnostics.ProcessStartInfo]::new()
$startInfo.FileName = $resolvedExecutable
$startInfo.WorkingDirectory = Split-Path -Parent $resolvedExecutable
$startInfo.UseShellExecute = $true

$process = [System.Diagnostics.Process]::Start($startInfo)
try {
    $root = Get-MainAutomationElement -Process $process
    $handle = $process.MainWindowHandle

    [NativeWindowCapture]::ShowWindow($handle, 9) | Out-Null
    [NativeWindowCapture]::SetWindowPos($handle, [IntPtr]::Zero, 24, 24, 2200, 1260, 0x0040) | Out-Null
    [NativeWindowCapture]::SetForegroundWindow($handle) | Out-Null
    Start-Sleep -Seconds 7
    Dismiss-MessageBoxes -ProcessId $process.Id
    Start-Sleep -Seconds 2

    Invoke-NamedButton -Root $root -Name 'Загрузить БД'
    Save-WindowScreenshot -WindowHandle $handle -FileName '01_load_database.png'

    Invoke-NamedButton -Root $root -Name 'Работа с БД'
    Save-WindowScreenshot -WindowHandle $handle -FileName '02_database_viewer.png'

    Invoke-NamedButton -Root $root -Name 'Угрозы'
    Save-WindowScreenshot -WindowHandle $handle -FileName '03_threats.png'
    Invoke-NamedButton -Root $root -Name 'Схема модели угроз'
    Save-WindowScreenshot -WindowHandle $handle -FileName '04_threat_scheme.png'

    Invoke-NamedButton -Root $root -Name 'Работа с БД'
    Invoke-NamedButton -Root $root -Name 'Нарушитель'
    Save-WindowScreenshot -WindowHandle $handle -FileName '05_violators.png'

    Invoke-NamedButton -Root $root -Name 'Работа с онтологиями'
    Save-WindowScreenshot -WindowHandle $handle -FileName '06_ontologies.png'

    Invoke-NamedButton -Root $root -Name 'Работа с моделями'
    Save-WindowScreenshot -WindowHandle $handle -FileName '07_models.png'

    Invoke-NamedButton -Root $root -Name 'Работа с ИЗ'
    Save-WindowScreenshot -WindowHandle $handle -FileName '08_knowledge_sources.png'

    Invoke-NamedButton -Root $root -Name 'Сформировать отчет'
    Save-WindowScreenshot -WindowHandle $handle -FileName '09_report.png'

    Invoke-NamedButton -Root $root -Name 'Общение с моделью'
    Save-WindowScreenshot -WindowHandle $handle -FileName '10_chat.png'
    Toggle-NamedButton -Root $root -Name '⚙'
    Save-WindowScreenshot -WindowHandle $handle -FileName '11_chat_settings.png'
    Select-SecondComboBoxItemByCurrentValue -Root $root -CurrentValue 'Обычный'
    Save-WindowScreenshot -WindowHandle $handle -FileName '13_arena_settings.png'
    Toggle-NamedButton -Root $root -Name 'Продвинутые'
    Save-WindowScreenshot -WindowHandle $handle -FileName '12_chat_advanced.png'
    Scroll-SettingsToBottom -Root $root
    Save-WindowScreenshot -WindowHandle $handle -FileName '14_arena_models.png'

    Invoke-NamedButton -Root $root -Name 'Информация'
    Save-WindowScreenshot -WindowHandle $handle -FileName '15_information.png'
}
finally {
    if ($process -and -not $process.HasExited) {
        $null = $process.CloseMainWindow()
        if (-not $process.WaitForExit(12000)) {
            $process.Kill($true)
            $process.WaitForExit()
        }
    }
}
