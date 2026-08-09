using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using PragmaticAnalyzer.MVVM.ViewModel.Viewer;

namespace PragmaticAnalyzer.MVVM.Views.Viewer
{
    public partial class ThreatView : UserControl
    {
        private const double MinThreatSchemeScale = 0.18;
        private const double MaxThreatSchemeScale = 3.0;
        private const double ThreatSchemeScaleStep = 0.1;
        private const double ThreatSchemeImageWidth = 3200.0;
        private const double ThreatSchemeImageHeight = 1221.0;
        private const double ThreatSchemeViewportPadding = 24.0;
        private const double VisioPageWidth = 22.88582677165354;
        private const double VisioPageHeight = 8.26771653543307;
        private const double EmbeddedSchemeLeft = 6.588576637184715;
        private const double EmbeddedSchemeBottom = 3.77382275432572;
        private const double EmbeddedSchemeWidth = 8.29173228346457;
        private const double EmbeddedSchemeHeight = 2.25;

        private static readonly IReadOnlyDictionary<string, string> ThreatNodeDescriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Угрозы, воздействующие на процесс функционирования СОПКА"] = "Центральный класс угроз, влияющих на нормальную работу средств и систем СОПКА. В схеме такие угрозы разделены на деструктивные непреднамеренные и деструктивные преднамеренные.",
            ["Деструктивные непреднамеренные"] = "Угрозы, возникающие без злого умысла: человеческие ошибки, сбои, отказы, внешние механические, электромагнитные и иные воздействия. Основной риск связан с нарушением устойчивости и доступности функционирования СОПКА.",
            ["Деструктивные преднамеренные"] = "Угрозы, реализуемые осознанно нарушителем или вредоносным кодом. В схеме они делятся на пассивные и активные воздействия.",
            ["Человеческий фактор"] = "Непреднамеренные угрозы, связанные с действиями людей: ошибками эксплуатации, неправильным применением средств, нарушением процедур или недостаточной подготовкой персонала.",
            ["Сбои и отказы СС"] = "Нарушения работоспособности средств системы, вызванные техническими сбоями, отказом компонентов, ошибками ПО, проблемами питания, сети или аппаратной части.",
            ["Ошибки применения СС СОПКА"] = "Ошибки использования средств СОПКА: неверный порядок применения, неправильная настройка, несвоевременное обслуживание, некорректная интерпретация сигналов и сообщений.",
            ["Механические воздействия"] = "Физические воздействия на оборудование: повреждение, вибрация, удар, разрушение корпуса, нарушение кабельных соединений или иных элементов инфраструктуры.",
            ["Электромагнитные воздействия"] = "Воздействия электромагнитного характера, способные нарушить работу оборудования, каналов передачи данных или точность обработки сигналов.",
            ["Воздействия другого характера"] = "Иные внешние факторы, не выделенные в отдельные группы: температура, влажность, загрязнение, пожар, затопление, нестабильность среды эксплуатации.",
            ["Воздействия окружающей среды"] = "Совокупность физических факторов окружающей среды, влияющих на работоспособность технических средств СОПКА.",
            ["Пассивные"] = "Преднамеренные угрозы, при которых нарушитель стремится получить информацию без прямого изменения работы системы. Обычно связаны с наблюдением, перехватом или анализом побочных каналов.",
            ["По ПЭМИН от СС СОПКА"] = "Пассивное получение информации по побочным электромагнитным излучениям и наводкам технических средств СОПКА.",
            ["По визуальным каналам"] = "Пассивное получение информации путем визуального наблюдения: экраны, документы, индикаторы, рабочие места, помещения или действия персонала.",
            ["Активные"] = "Преднамеренные угрозы, при которых нарушитель воздействует на систему напрямую: заражает, атакует, изменяет данные, блокирует работу или нарушает управление.",
            ["Вирусное заражение СС СОПКА"] = "Активная угроза внедрения вредоносного программного обеспечения в средства СОПКА. Может привести к нарушению работы, утечке данных, подмене результатов или несанкционированному управлению.",
            ["Компьютерные атаки на СС СОПКА"] = "Активные сетевые или локальные атаки на средства СОПКА: эксплуатация уязвимостей, подбор учетных данных, удаленное выполнение кода, нарушение доступности или изменение конфигурации.",
            ["Ошибки администрирования"] = "Ошибки настройки, сопровождения или управления средствами СОПКА: неверные права доступа, некорректные политики, неучтенные изменения конфигурации, отключенное журналирование.",
            ["Ошибки пользователей"] = "Неверные действия пользователей при эксплуатации системы: запуск опасных файлов, ошибочный ввод данных, нарушение порядка работы или игнорирование предупреждений.",
            ["Нарушение регламентов"] = "Несоблюдение утвержденных процедур эксплуатации и безопасности. Может приводить к обходу защитных мер и появлению неконтролируемых сценариев работы.",
            ["Социальная инженерия"] = "Воздействие на персонал через обман: фишинговые сообщения, поддельные обращения, просьбы раскрыть данные или выполнить вредоносное действие.",
            ["Баги и уязвимости ПО"] = "Ошибки и уязвимости программного обеспечения, которые могут нарушить работу средств СОПКА или быть использованы нарушителем.",
            ["Несовместимость ПО"] = "Конфликты версий, библиотек, драйверов или компонентов, способные привести к сбоям и отказам в работе системы.",
            ["Аппаратные отказы"] = "Отказы технических средств: серверов, рабочих станций, сетевого оборудования, накопителей, источников питания и других компонентов.",
            ["Аварии электроснабжения"] = "Нарушения питания, которые могут привести к остановке оборудования, повреждению данных или некорректному завершению процессов.",
            ["Пожары/потопы"] = "Физические аварии, приводящие к повреждению помещений, оборудования, кабельной инфраструктуры и носителей информации.",
            ["Эксплойты"] = "Использование программного кода или техник эксплуатации уязвимостей для получения доступа, нарушения работы или закрепления в системе.",
            ["Атаки типа «Отказ в обслуживании»"] = "Атаки, направленные на нарушение доступности сервисов, каналов связи или компонентов СОПКА.",
            ["Атаки типа «Человек посередине»"] = "Перехват или изменение обмена данными между участниками взаимодействия, позволяющие читать, подменять или перенаправлять трафик.",
            ["Атаки с применением технологий ИИ"] = "Использование ИИ для автоматизации атак, генерации фишинга, обхода фильтров, поиска уязвимостей или усиления вредоносного ПО."
        };

        private bool _isThreatSchemeVisible;
        private double _threatSchemeScale = 1.0;
        private readonly Dictionary<string, List<Rect>> _threatSchemeNodeBounds = new(StringComparer.OrdinalIgnoreCase);

        public ThreatView()
        {
            InitializeComponent();
            InitializeThreatSchemeHotspots();
        }

        private void ThreatView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ThreatViewModel viewModel)
            {
                viewModel.EnsureSelection();
            }

            ThreatIdentifiersListBox.Items.Refresh();
        }

        private void ToggleThreatSchemeButton_Click(object sender, RoutedEventArgs e)
        {
            SetThreatSchemeVisible(!_isThreatSchemeVisible);
        }

        private void HideThreatSchemeButton_Click(object sender, RoutedEventArgs e)
        {
            SetThreatSchemeVisible(false);
        }

        private void SetThreatSchemeVisible(bool isVisible)
        {
            _isThreatSchemeVisible = isVisible;

            ThreatCardsPanel.Visibility = isVisible
                ? Visibility.Collapsed
                : Visibility.Visible;

            ThreatSchemePanel.Visibility = isVisible
                ? Visibility.Visible
                : Visibility.Collapsed;

            Grid.SetRow(ThreatContentBorder, isVisible ? 0 : 1);
            Grid.SetColumn(ThreatContentBorder, isVisible ? 0 : 1);
            Grid.SetRowSpan(ThreatContentBorder, isVisible ? 4 : 3);
            Grid.SetColumnSpan(ThreatContentBorder, isVisible ? 3 : 1);
            Panel.SetZIndex(ThreatContentBorder, isVisible ? 20 : 1);

            ThreatSchemeToggleButton.Content = isVisible
                ? "Карточка угрозы"
                : "Схема модели угроз";

            if (isVisible)
            {
                Dispatcher.BeginInvoke(new Action(FitThreatSchemeToViewport), DispatcherPriority.Loaded);
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (DataContext is ThreatViewModel viewModel)
                    {
                        viewModel.EnsureSelection();
                    }

                    ThreatIdentifiersListBox.Items.Refresh();
                }), DispatcherPriority.Loaded);
            }
        }

        private void ThreatSchemeScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                ChangeThreatSchemeScale(ThreatSchemeScaleStep);
            }
            else
            {
                ChangeThreatSchemeScale(-ThreatSchemeScaleStep);
            }

            e.Handled = true;
        }

        private void ResetThreatSchemeZoomButton_Click(object sender, RoutedEventArgs e)
        {
            _threatSchemeScale = 1.0;
            ApplyThreatSchemeScale();
        }

        private void ChangeThreatSchemeScale(double delta)
        {
            _threatSchemeScale += delta;

            if (_threatSchemeScale < MinThreatSchemeScale)
            {
                _threatSchemeScale = MinThreatSchemeScale;
            }

            if (_threatSchemeScale > MaxThreatSchemeScale)
            {
                _threatSchemeScale = MaxThreatSchemeScale;
            }

            ApplyThreatSchemeScale();
        }

        private void ApplyThreatSchemeScale()
        {
            ThreatSchemeScaleTransform.ScaleX = _threatSchemeScale;
            ThreatSchemeScaleTransform.ScaleY = _threatSchemeScale;
        }

        private void FitThreatSchemeToViewport()
        {
            var availableWidth = ThreatSchemeScrollViewer.ActualWidth - ThreatSchemeViewportPadding;
            var availableHeight = ThreatSchemeScrollViewer.ActualHeight - ThreatSchemeViewportPadding;

            if (availableWidth <= 0 || availableHeight <= 0)
            {
                return;
            }

            var scaleByWidth = availableWidth / ThreatSchemeImageWidth;
            var scaleByHeight = availableHeight / ThreatSchemeImageHeight;
            _threatSchemeScale = Math.Min(scaleByWidth, scaleByHeight);

            if (_threatSchemeScale < MinThreatSchemeScale)
            {
                _threatSchemeScale = MinThreatSchemeScale;
            }

            if (_threatSchemeScale > MaxThreatSchemeScale)
            {
                _threatSchemeScale = MaxThreatSchemeScale;
            }

            ApplyThreatSchemeScale();
            ThreatSchemeScrollViewer.ScrollToHorizontalOffset(0);
            ThreatSchemeScrollViewer.ScrollToVerticalOffset(0);
        }

        private void ThreatSchemeNodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
            {
                return;
            }

            var nodeName = button.Tag?.ToString() ?? "Неизвестный блок";
            ThreatSchemeNodeInfoTitle.Text = nodeName;
            ThreatSchemeNodeInfoDescription.Text = GetThreatNodeDescription(nodeName);
        }

        private static string GetThreatNodeDescription(string nodeName)
        {
            if (ThreatNodeDescriptions.TryGetValue(nodeName, out var description))
            {
                return description;
            }

            return $"Узел «{nodeName}» относится к модели угроз информационной безопасности. Его нужно рассматривать как возможную причину, сценарий или последствие инцидента и связывать с нарушением конфиденциальности, целостности или доступности.";
        }

        private void InitializeThreatSchemeHotspots()
        {
            _threatSchemeNodeBounds.Clear();
            ThreatSchemeLinesCanvas.Children.Clear();
            ThreatSchemeHotspotsCanvas.Children.Clear();

            AddEmbeddedImageHotspots();
            AddVisioPageHotspots();
            AddThreatSchemeLegend();
            AddThreatSchemeConnectors();
        }

        private void AddEmbeddedImageHotspots()
        {
            AddEmbeddedHotspot("Человеческий фактор", 4, 8, 274, 156);
            AddEmbeddedHotspot("Сбои и отказы СС", 4, 192, 274, 156);
            AddEmbeddedHotspot("Механические воздействия", 4, 376, 274, 156);
            AddEmbeddedHotspot("Электромагнитные воздействия", 4, 555, 274, 156);
            AddEmbeddedHotspot("Воздействия другого характера", 4, 738, 274, 156);
            AddEmbeddedHotspot("Ошибки применения СС СОПКА", 310, 96, 148, 156);
            AddEmbeddedHotspot("Воздействия окружающей среды", 310, 554, 148, 158);
            AddEmbeddedHotspot("Деструктивные непреднамеренные", 491, 313, 209, 216);
            AddEmbeddedHotspot("Угрозы, воздействующие на процесс функционирования СОПКА", 716, 207, 203, 428);
            AddEmbeddedHotspot("Деструктивные преднамеренные", 934, 313, 198, 216);
            AddEmbeddedHotspot("Пассивные", 1166, 130, 148, 156);
            AddEmbeddedHotspot("По ПЭМИН от СС СОПКА", 1341, 36, 256, 156);
            AddEmbeddedHotspot("По визуальным каналам", 1341, 221, 256, 157);
            AddEmbeddedHotspot("Активные", 1166, 550, 148, 156);
            AddEmbeddedHotspot("Вирусное заражение СС СОПКА", 1341, 443, 256, 157);
            AddEmbeddedHotspot("Компьютерные атаки на СС СОПКА", 1341, 626, 256, 157);
        }

        private void AddVisioPageHotspots()
        {
            AddPageHotspot("Ошибки администрирования", 7.81526513386846, 7.82086613488589, 1.67716541266197, 0.393700801094358);
            AddPageHotspot("Излучение от мониторов", 13.5751077797402, 7.45036789052924, 1.67716541266197, 0.393700801094358);
            AddPageHotspot("Ошибки пользователей", 7.81526513386846, 7.3785730465924, 1.67716541266197, 0.393700801094358);
            AddPageHotspot("Наводки по кабелям заземления и связи", 13.5751077797402, 6.95708384449148, 1.67716541266197, 0.393700801094358);
            AddPageHotspot("Нарушение регламентов", 7.81443609029897, 6.9223443395095, 1.67882349980094, 0.421572038673188);
            AddPageHotspot("Социальная инженерия", 7.81360704672949, 6.4661156324266, 1.67716541266197, 0.393700801094358);
            AddPageHotspot("Наводки по линиям питания", 13.5751077797402, 6.46379979845372, 1.67716541266197, 0.393700801094358);
            AddPageHotspot("Несовместимость ПО", 5.2089657963889, 6.2880753273563, 1.67716541266197, 0.393700801094358);
            AddPageHotspot("Наблюдение через оптические средства", 16.1932177929561, 6.269265457371, 1.67716541266197, 0.393700801094358);
            AddPageHotspot("Аппаратные отказы", 5.2089657963889, 5.82138368886102, 1.67716541266197, 0.393700801094358);
            AddPageHotspot("Прямое визуальное наблюдение", 16.1932177929561, 5.77598141133324, 1.67716541266197, 0.393700801094358);
            AddPageHotspot("Баги и уязвимости ПО", 5.2089657963889, 5.35469205036574, 1.67716541266197, 0.393700801094358);
            AddPageHotspot("Использование скрытых камер", 16.1932177929561, 5.28269736529548, 1.67716541266197, 0.393700801094358);
            AddPageHotspot("Удары", 5.2089657963889, 4.88800041187047, 1.67716541266197, 0.393700801094358);
            AddPageHotspot("Черви, трояны и другие вирусы", 16.1932177929561, 4.71183116537088, 1.67716541266197, 0.393700801094358);
            AddPageHotspot("Вибрации", 5.2089657963889, 4.42130877337519, 1.67716541266197, 0.393700801094358);
            AddPageHotspot("Шпионское ПО", 16.1932177929561, 4.20233625063179, 1.67716541266197, 0.393700801094358);
            AddPageHotspot("Электромагнитные импульсы", 5.2089657963889, 3.95461713487991, 1.67716541266197, 0.393700801094358);
            AddPageHotspot("Эксплойты", 16.1932177929561, 3.69284133589271, 1.67716541266197, 0.393700801094358);
            AddPageHotspot("Радиочастотные помехи", 5.2089657963889, 3.48792549638463, 1.67716541266197, 0.393700801094358);
            AddPageHotspot("Окружающий климат", 7.81526513386846, 3.31650933899865, 1.67716541266197, 0.393700801094358);
            AddPageHotspot("Атаки на доступ (брутфорс и тп)", 13.6262888838825, 3.2672968643142, 1.67716541266197, 0.393700801094358);
            AddPageHotspot("Другие воздействия", 16.1932177929561, 3.21808425783672, 1.67716541266197, 0.393700801094358);
            AddPageHotspot("Статическое электричество", 5.2089657963889, 3.02123385788935, 1.67716541266197, 0.393700801094358);
            AddPageHotspot("Аварии электроснабжения", 7.81526513386846, 2.84282303280494, 1.67716541266197, 0.393700801094358);
            AddPageHotspot("Атаки типа «Отказ в обслуживании»", 13.6262888838825, 2.77517087267125, 1.67716541266197, 0.393700801094358);
            AddPageHotspot("ИИ в фишинге и соц инженерии", 19.1847347535732, 2.57832036907469, 1.67716541266197, 0.393700801094358);
            AddPageHotspot("Пожары/потопы", 7.81526513386846, 2.36913672661122, 1.67716541266197, 0.393700801094358);
            AddPageHotspot("Атаки типа «Человек посередине»", 13.6262888838825, 2.28304488102828, 1.67716541266197, 0.393700801094358);
            AddPageHotspot("Отравление обучающих данных", 19.1847347535732, 2.0688254543356, 1.67716541266197, 0.393700801094358);
            AddPageHotspot("Химические коррозия материала", 7.81526513386846, 1.89545042041751, 1.67716541266197, 0.393700801094358);
            AddPageHotspot("Атаки других типов", 13.6144777727771, 1.82200042714729, 1.67716541266197, 0.393700801094358);
            AddPageHotspot("Атаки с применением технологий ИИ", 16.1459739627338, 1.82200042714729, 1.67716541266197, 0.393700801094358);
            AddPageHotspot("ИИ в ВПО", 19.1847347535732, 1.55933053959652, 1.67716541266197, 0.393700801094358);
            AddPageHotspot("ИИ в DoS-атаках", 19.1847347535732, 1.08457346154053, 1.67716541266197, 0.393700801094358);
            AddPageHotspot("ИИ в фишинге и соц инженерии", 19.1847347535732, 0.629921278763456, 1.67716541266197, 0.393700801094358);
        }

        private void AddThreatSchemeLegend()
        {
            const string legendText =
                "Перечень используемых сокращений\n" +
                "СОПКА - система обнаружения, предупреждения и ликвидации последствий компьютерных атак\n" +
                "СС СОПКА - системные средства системы обнаружения, предупреждения и ликвидации последствий компьютерных атак\n" +
                "ПО - программное обеспечение\n" +
                "ПЭМИН - побочные электромагнитные излучения и наводки\n" +
                "ИИ - искусственный интеллект\n" +
                "ВПО - вредоносное программное обеспечение";

            AddPageTextBox(legendText, 3.147637679824033, 1.442964692744934, 5.460629782457806, 1.97543592700576);
        }

        private void AddThreatSchemeConnectors()
        {
            AddConnector("Угрозы, воздействующие на процесс функционирования СОПКА", "Деструктивные непреднамеренные");
            AddConnector("Угрозы, воздействующие на процесс функционирования СОПКА", "Деструктивные преднамеренные");

            AddConnector("Деструктивные непреднамеренные", "Ошибки применения СС СОПКА");
            AddConnector("Деструктивные непреднамеренные", "Воздействия окружающей среды");
            AddConnector("Ошибки применения СС СОПКА", "Человеческий фактор");
            AddConnector("Ошибки применения СС СОПКА", "Сбои и отказы СС");
            AddConnector("Воздействия окружающей среды", "Механические воздействия");
            AddConnector("Воздействия окружающей среды", "Электромагнитные воздействия");
            AddConnector("Воздействия окружающей среды", "Воздействия другого характера");

            AddConnector("Человеческий фактор", "Ошибки администрирования");
            AddConnector("Человеческий фактор", "Ошибки пользователей");
            AddConnector("Человеческий фактор", "Нарушение регламентов");
            AddConnector("Человеческий фактор", "Социальная инженерия");

            AddConnector("Сбои и отказы СС", "Несовместимость ПО");
            AddConnector("Сбои и отказы СС", "Аппаратные отказы");
            AddConnector("Сбои и отказы СС", "Баги и уязвимости ПО");

            AddConnector("Механические воздействия", "Удары");
            AddConnector("Механические воздействия", "Вибрации");
            AddConnector("Электромагнитные воздействия", "Электромагнитные импульсы");
            AddConnector("Электромагнитные воздействия", "Радиочастотные помехи");
            AddConnector("Электромагнитные воздействия", "Статическое электричество");
            AddConnector("Воздействия другого характера", "Окружающий климат");
            AddConnector("Воздействия другого характера", "Аварии электроснабжения");
            AddConnector("Воздействия другого характера", "Пожары/потопы");
            AddConnector("Воздействия другого характера", "Химические коррозия материала");

            AddConnector("Деструктивные преднамеренные", "Пассивные");
            AddConnector("Деструктивные преднамеренные", "Активные");
            AddConnector("Пассивные", "По ПЭМИН от СС СОПКА");
            AddConnector("Пассивные", "По визуальным каналам");
            AddConnector("Активные", "Вирусное заражение СС СОПКА");
            AddConnector("Активные", "Компьютерные атаки на СС СОПКА");

            AddConnector("По ПЭМИН от СС СОПКА", "Излучение от мониторов");
            AddConnector("По ПЭМИН от СС СОПКА", "Наводки по кабелям заземления и связи");
            AddConnector("По ПЭМИН от СС СОПКА", "Наводки по линиям питания");
            AddConnector("По визуальным каналам", "Наблюдение через оптические средства");
            AddConnector("По визуальным каналам", "Прямое визуальное наблюдение");
            AddConnector("По визуальным каналам", "Использование скрытых камер");

            AddConnector("Вирусное заражение СС СОПКА", "Черви, трояны и другие вирусы");
            AddConnector("Вирусное заражение СС СОПКА", "Шпионское ПО");
            AddConnector("Вирусное заражение СС СОПКА", "Эксплойты");
            AddConnector("Вирусное заражение СС СОПКА", "Другие воздействия");

            AddConnector("Компьютерные атаки на СС СОПКА", "Атаки на доступ (брутфорс и тп)");
            AddConnector("Компьютерные атаки на СС СОПКА", "Атаки типа «Отказ в обслуживании»");
            AddConnector("Компьютерные атаки на СС СОПКА", "Атаки типа «Человек посередине»");
            AddConnector("Компьютерные атаки на СС СОПКА", "Атаки других типов");
            AddConnector("Компьютерные атаки на СС СОПКА", "Атаки с применением технологий ИИ");

            AddConnector("Атаки с применением технологий ИИ", "ИИ в фишинге и соц инженерии", targetIndex: 0);
            AddConnector("Атаки с применением технологий ИИ", "Отравление обучающих данных");
            AddConnector("Атаки с применением технологий ИИ", "ИИ в ВПО");
            AddConnector("Атаки с применением технологий ИИ", "ИИ в DoS-атаках");
            AddConnector("Атаки с применением технологий ИИ", "ИИ в фишинге и соц инженерии", targetIndex: 1);
        }

        private void AddEmbeddedHotspot(string title, double left, double top, double width, double height)
        {
            var pageLeft = EmbeddedSchemeLeft + EmbeddedSchemeWidth * left / 1600.0;
            var pageTop = EmbeddedSchemeBottom + EmbeddedSchemeHeight * (1.0 - top / 900.0);
            var pageWidth = EmbeddedSchemeWidth * width / 1600.0;
            var pageHeight = EmbeddedSchemeHeight * height / 900.0;
            var pageCenterX = pageLeft + pageWidth / 2.0;
            var pageCenterY = pageTop - pageHeight / 2.0;
            AddPageHotspot(title, pageCenterX, pageCenterY, pageWidth, pageHeight);
        }

        private void AddPageHotspot(string title, double pinX, double pinY, double width, double height)
        {
            var left = (pinX - width / 2.0) / VisioPageWidth * ThreatSchemeImageWidth;
            var top = (VisioPageHeight - pinY - height / 2.0) / VisioPageHeight * ThreatSchemeImageHeight;
            var buttonWidth = width / VisioPageWidth * ThreatSchemeImageWidth;
            var buttonHeight = height / VisioPageHeight * ThreatSchemeImageHeight;
            AddThreatHotspot(title, left, top, buttonWidth, buttonHeight);
        }

        private void AddPageTextBox(string text, double pinX, double pinY, double width, double height)
        {
            var left = (pinX - width / 2.0) / VisioPageWidth * ThreatSchemeImageWidth;
            var top = (VisioPageHeight - pinY - height / 2.0) / VisioPageHeight * ThreatSchemeImageHeight;
            var boxWidth = width / VisioPageWidth * ThreatSchemeImageWidth;
            var boxHeight = height / VisioPageHeight * ThreatSchemeImageHeight;

            var border = new Border
            {
                Width = boxWidth,
                Height = boxHeight,
                Background = Brushes.White,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(2),
                Padding = new Thickness(14, 10, 14, 10),
                Child = new TextBlock
                {
                    Text = text,
                    FontSize = 21,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.Black,
                    TextWrapping = TextWrapping.Wrap
                }
            };

            Canvas.SetLeft(border, left);
            Canvas.SetTop(border, top);
            ThreatSchemeHotspotsCanvas.Children.Add(border);
        }

        private void AddThreatHotspot(string title, double left, double top, double width, double height)
        {
            var button = new Button
            {
                Style = (Style)FindResource("ThreatSchemeHotspotButtonStyle"),
                Tag = title,
                ToolTip = title,
                Content = new TextBlock
                {
                    Text = title,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontSize = GetThreatSchemeFontSize(width, height, title),
                    FontWeight = FontWeights.SemiBold,
                    LineHeight = GetThreatSchemeLineHeight(width, height, title),
                    LineStackingStrategy = LineStackingStrategy.BlockLineHeight
                },
                Width = Math.Max(width, 22),
                Height = Math.Max(height, 18)
            };

            button.Background = IsMajorThreatSchemeNode(title, width, height)
                ? new SolidColorBrush(Color.FromRgb(220, 232, 194))
                : new SolidColorBrush(Color.FromRgb(241, 247, 222));

            button.Click += ThreatSchemeNodeButton_Click;
            Canvas.SetLeft(button, left);
            Canvas.SetTop(button, top);
            ThreatSchemeHotspotsCanvas.Children.Add(button);

            if (!_threatSchemeNodeBounds.TryGetValue(title, out var bounds))
            {
                bounds = new List<Rect>();
                _threatSchemeNodeBounds[title] = bounds;
            }

            bounds.Add(new Rect(left, top, button.Width, button.Height));
        }

        private static double GetThreatSchemeFontSize(double width, double height, string title)
        {
            if (title.Length > 55 || width < 150 || height > 120)
            {
                return 16;
            }

            if (title.Length > 32)
            {
                return 17;
            }

            return 18;
        }

        private static double GetThreatSchemeLineHeight(double width, double height, string title)
        {
            return GetThreatSchemeFontSize(width, height, title) + 2;
        }

        private static bool IsMajorThreatSchemeNode(string title, double width, double height)
        {
            return height > 100
                   || title is "Угрозы, воздействующие на процесс функционирования СОПКА"
                       or "Деструктивные непреднамеренные"
                       or "Деструктивные преднамеренные"
                       or "Пассивные"
                       or "Активные";
        }

        private void AddConnector(string sourceTitle, string targetTitle, int sourceIndex = 0, int targetIndex = 0)
        {
            if (!TryGetNodeRect(sourceTitle, sourceIndex, out var source)
                || !TryGetNodeRect(targetTitle, targetIndex, out var target))
            {
                return;
            }

            var sourceCenter = GetCenter(source);
            var targetCenter = GetCenter(target);
            var start = new Point(
                targetCenter.X >= sourceCenter.X ? source.Right : source.Left,
                sourceCenter.Y);
            var end = new Point(
                targetCenter.X >= sourceCenter.X ? target.Left : target.Right,
                targetCenter.Y);
            var middleX = start.X + (end.X - start.X) / 2.0;

            var line = new Polyline
            {
                Stroke = Brushes.Black,
                StrokeThickness = 4,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Points = new PointCollection
                {
                    start,
                    new Point(middleX, start.Y),
                    new Point(middleX, end.Y),
                    end
                }
            };

            ThreatSchemeLinesCanvas.Children.Add(line);
        }

        private bool TryGetNodeRect(string title, int index, out Rect rect)
        {
            rect = Rect.Empty;

            if (!_threatSchemeNodeBounds.TryGetValue(title, out var bounds) || bounds.Count == 0)
            {
                return false;
            }

            rect = bounds[Math.Clamp(index, 0, bounds.Count - 1)];
            return true;
        }

        private static Point GetCenter(Rect rect)
        {
            return new Point(rect.Left + rect.Width / 2.0, rect.Top + rect.Height / 2.0);
        }
    }
}
