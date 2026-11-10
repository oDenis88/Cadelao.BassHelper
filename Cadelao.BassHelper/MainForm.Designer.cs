using Cadelao.BassHelper.UI;

namespace Cadelao.BassHelper;

partial class MainForm
{
    private System.ComponentModel.IContainer? components = null;
    private TableLayoutPanel rootLayout = null!;
    private Panel headerPanel = null!;
    private Label lblTitle = null!;
    private FlowLayoutPanel toolbar = null!;
    private Button btnOpen = null!;
    private TransportIconButton btnPlay = null!;
    private TransportIconButton btnPause = null!;
    private TransportIconButton btnStop = null!;
    private Button btnExport = null!;
    private Panel panelTransport = null!;
    private Label lblFile = null!;
    private Label lblTime = null!;
    private Label lblDuration = null!;
    private TrackBar trackSeek = null!;
    private TrackBar trackVolume = null!;
    private Label lblVolume = null!;
    private NumericUpDown numSync = null!;
    private Label lblSync = null!;
    private Panel panelTimeline = null!;
    private Label lblTimeline = null!;
    private PhraseTimelineControl timeline = null!;
    private TableLayoutPanel timelineLayout = null!;
    private Panel timelineHeader = null!;
    private Label lblTimelineZoom = null!;
    private NumericUpDown numTimelineZoom = null!;
    private Button btnTimelineFollow = null!;
    private HScrollBar scrollTimeline = null!;
    private TableLayoutPanel centerLayout = null!;
    private TableLayoutPanel leftCenterLayout = null!;
    private Panel panelNow = null!;
    private Label lblNowTitle = null!;
    private Label lblCurrent = null!;
    private Label lblQuality = null!;
    private Label lblTriad = null!;
    private Label lblShape = null!;
    private Label lblBass = null!;
    private Label lblNext = null!;
    private Label lblEvidence = null!;
    private BassFretboardControl fretboard = null!;
    private Panel panelGrid = null!;
    private Label lblGrid = null!;
    private DataGridView gridChords = null!;
    private DataGridViewTextBoxColumn colStart = null!;
    private DataGridViewTextBoxColumn colEnd = null!;
    private DataGridViewTextBoxColumn colChord = null!;
    private DataGridViewTextBoxColumn colConfidence = null!;
    private Panel footer = null!;
    private Label lblStatus = null!;
    private ProgressBar progressAnalysis = null!;
    private System.Windows.Forms.Timer timerPlayhead = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DisposeResources();
            components?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
        rootLayout = new TableLayoutPanel();
        headerPanel = new Panel();
        lblTitle = new Label();
        toolbar = new FlowLayoutPanel();
        btnOpen = new Button();
        btnPlay = new TransportIconButton();
        btnPause = new TransportIconButton();
        btnStop = new TransportIconButton();
        btnExport = new Button();
        panelTransport = new Panel();
        lblFile = new Label();
        lblTime = new Label();
        lblDuration = new Label();
        trackSeek = new TrackBar();
        trackVolume = new TrackBar();
        lblVolume = new Label();
        panelTimeline = new Panel();
        timelineLayout = new TableLayoutPanel();
        timelineHeader = new Panel();
        lblTimeline = new Label();
        timeline = new PhraseTimelineControl();
        scrollTimeline = new HScrollBar();
        centerLayout = new TableLayoutPanel();
        leftCenterLayout = new TableLayoutPanel();
        panelNow = new Panel();
        lblNowTitle = new Label();
        lblCurrent = new Label();
        lblQuality = new Label();
        lblTriad = new Label();
        lblShape = new Label();
        lblNext = new Label();
        lblBass = new Label();
        lblEvidence = new Label();
        panelGrid = new Panel();
        lblGrid = new Label();
        gridChords = new DataGridView();
        colStart = new DataGridViewTextBoxColumn();
        colEnd = new DataGridViewTextBoxColumn();
        colChord = new DataGridViewTextBoxColumn();
        colConfidence = new DataGridViewTextBoxColumn();
        fretboard = new BassFretboardControl();
        footer = new Panel();
        lblStatus = new Label();
        progressAnalysis = new ProgressBar();
        numSync = new NumericUpDown();
        lblSync = new Label();
        lblTimelineZoom = new Label();
        numTimelineZoom = new NumericUpDown();
        btnTimelineFollow = new Button();
        timerPlayhead = new System.Windows.Forms.Timer(components);
        rootLayout.SuspendLayout();
        headerPanel.SuspendLayout();
        toolbar.SuspendLayout();
        panelTransport.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)trackSeek).BeginInit();
        ((System.ComponentModel.ISupportInitialize)trackVolume).BeginInit();
        panelTimeline.SuspendLayout();
        timelineLayout.SuspendLayout();
        timelineHeader.SuspendLayout();
        centerLayout.SuspendLayout();
        leftCenterLayout.SuspendLayout();
        panelNow.SuspendLayout();
        panelGrid.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)gridChords).BeginInit();
        footer.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)numSync).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numTimelineZoom).BeginInit();
        SuspendLayout();
        // 
        // rootLayout
        // 
        rootLayout.ColumnCount = 1;
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayout.Controls.Add(headerPanel, 0, 0);
        rootLayout.Controls.Add(toolbar, 0, 1);
        rootLayout.Controls.Add(panelTransport, 0, 2);
        rootLayout.Controls.Add(panelTimeline, 0, 3);
        rootLayout.Controls.Add(centerLayout, 0, 4);
        rootLayout.Controls.Add(footer, 0, 5);
        rootLayout.Dock = DockStyle.Fill;
        rootLayout.Location = new Point(0, 0);
        rootLayout.Margin = new Padding(0);
        rootLayout.Name = "rootLayout";
        rootLayout.Padding = new Padding(12, 7, 12, 7);
        rootLayout.RowCount = 6;
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 181F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        rootLayout.Size = new Size(1480, 900);
        rootLayout.TabIndex = 0;
        // 
        // headerPanel
        // 
        headerPanel.Controls.Add(lblTitle);
        headerPanel.Dock = DockStyle.Fill;
        headerPanel.Location = new Point(15, 10);
        headerPanel.Name = "headerPanel";
        headerPanel.Size = new Size(1450, 52);
        headerPanel.TabIndex = 0;
        // 
        // lblTitle
        // 
        lblTitle.AutoSize = true;
        lblTitle.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
        lblTitle.ForeColor = Color.FromArgb(110, 226, 206);
        lblTitle.Location = new Point(2, 1);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(237, 41);
        lblTitle.TabIndex = 0;
        lblTitle.Text = "CADELÃO BASS";
        // 
        // toolbar
        // 
        toolbar.AutoScroll = true;
        toolbar.Controls.Add(btnOpen);
        toolbar.Controls.Add(btnPlay);
        toolbar.Controls.Add(btnPause);
        toolbar.Controls.Add(btnStop);
        toolbar.Controls.Add(btnExport);
        toolbar.Dock = DockStyle.Fill;
        toolbar.Location = new Point(12, 65);
        toolbar.Margin = new Padding(0);
        toolbar.Name = "toolbar";
        toolbar.Padding = new Padding(0, 2, 0, 2);
        toolbar.Size = new Size(1456, 60);
        toolbar.TabIndex = 1;
        toolbar.WrapContents = false;
        // 
        // btnOpen
        // 
        btnOpen.BackColor = Color.FromArgb(31, 156, 155);
        btnOpen.FlatAppearance.BorderSize = 0;
        btnOpen.FlatStyle = FlatStyle.Flat;
        btnOpen.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnOpen.ForeColor = Color.White;
        btnOpen.Location = new Point(1, 4);
        btnOpen.Margin = new Padding(1, 2, 14, 2);
        btnOpen.Name = "btnOpen";
        btnOpen.Size = new Size(176, 44);
        btnOpen.TabIndex = 0;
        btnOpen.Text = "ABRIR MP3 / WAV";
        btnOpen.UseVisualStyleBackColor = false;
        // 
        // btnPlay
        // 
        btnPlay.BackColor = Color.FromArgb(26, 120, 93);
        btnPlay.FlatStyle = FlatStyle.Flat;
        btnPlay.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnPlay.ForeColor = Color.White;
        btnPlay.Location = new Point(193, 4);
        btnPlay.Margin = new Padding(2, 2, 5, 2);
        btnPlay.Name = "btnPlay";
        btnPlay.Size = new Size(118, 44);
        btnPlay.TabIndex = 1;
        btnPlay.Text = "TOCAR";
        btnPlay.UseVisualStyleBackColor = false;
        // 
        // btnPause
        // 
        btnPause.BackColor = Color.FromArgb(50, 84, 143);
        btnPause.FlatStyle = FlatStyle.Flat;
        btnPause.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnPause.ForeColor = Color.White;
        btnPause.Location = new Point(318, 4);
        btnPause.Margin = new Padding(2, 2, 5, 2);
        btnPause.Name = "btnPause";
        btnPause.Size = new Size(126, 44);
        btnPause.Symbol = TransportSymbol.Pause;
        btnPause.TabIndex = 2;
        btnPause.Text = "PAUSAR";
        btnPause.UseVisualStyleBackColor = false;
        // 
        // btnStop
        // 
        btnStop.BackColor = Color.FromArgb(133, 66, 77);
        btnStop.FlatStyle = FlatStyle.Flat;
        btnStop.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnStop.ForeColor = Color.White;
        btnStop.Location = new Point(451, 4);
        btnStop.Margin = new Padding(2, 2, 23, 2);
        btnStop.Name = "btnStop";
        btnStop.Size = new Size(117, 44);
        btnStop.Symbol = TransportSymbol.Stop;
        btnStop.TabIndex = 3;
        btnStop.Text = "PARAR";
        btnStop.UseVisualStyleBackColor = false;
        // 
        // btnExport
        // 
        btnExport.BackColor = Color.FromArgb(44, 58, 76);
        btnExport.FlatAppearance.BorderSize = 0;
        btnExport.FlatStyle = FlatStyle.Flat;
        btnExport.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnExport.ForeColor = Color.White;
        btnExport.Location = new Point(593, 4);
        btnExport.Margin = new Padding(2, 2, 5, 2);
        btnExport.Name = "btnExport";
        btnExport.Size = new Size(177, 44);
        btnExport.TabIndex = 4;
        btnExport.Text = "EXPORTAR CIFRAS";
        btnExport.UseVisualStyleBackColor = false;
        // 
        // panelTransport
        // 
        panelTransport.BackColor = Color.FromArgb(22, 30, 44);
        panelTransport.Controls.Add(lblFile);
        panelTransport.Controls.Add(lblTime);
        panelTransport.Controls.Add(lblDuration);
        panelTransport.Controls.Add(trackSeek);
        panelTransport.Controls.Add(trackVolume);
        panelTransport.Controls.Add(lblVolume);
        panelTransport.Dock = DockStyle.Fill;
        panelTransport.Location = new Point(12, 125);
        panelTransport.Margin = new Padding(0, 0, 0, 7);
        panelTransport.Name = "panelTransport";
        panelTransport.Size = new Size(1456, 93);
        panelTransport.TabIndex = 2;
        // 
        // lblFile
        // 
        lblFile.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lblFile.ForeColor = Color.FromArgb(190, 208, 229);
        lblFile.Location = new Point(15, 8);
        lblFile.Name = "lblFile";
        lblFile.Size = new Size(2036, 21);
        lblFile.TabIndex = 0;
        lblFile.Text = "Abra uma musica para comecar";
        // 
        // lblTime
        // 
        lblTime.AutoSize = true;
        lblTime.Font = new Font("Consolas", 11F, FontStyle.Bold);
        lblTime.ForeColor = Color.White;
        lblTime.Location = new Point(17, 42);
        lblTime.Name = "lblTime";
        lblTime.Size = new Size(48, 18);
        lblTime.TabIndex = 1;
        lblTime.Text = "00:00";
        // 
        // lblDuration
        // 
        lblDuration.AutoSize = true;
        lblDuration.ForeColor = Color.FromArgb(147, 170, 195);
        lblDuration.Location = new Point(17, 70);
        lblDuration.Name = "lblDuration";
        lblDuration.Size = new Size(34, 15);
        lblDuration.TabIndex = 2;
        lblDuration.Text = "00:00";
        // 
        // trackSeek
        // 
        trackSeek.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        trackSeek.Enabled = false;
        trackSeek.Location = new Point(99, 43);
        trackSeek.Maximum = 10000;
        trackSeek.Name = "trackSeek";
        trackSeek.Size = new Size(1981, 45);
        trackSeek.TabIndex = 3;
        trackSeek.TickStyle = TickStyle.None;
        // 
        // trackVolume
        // 
        trackVolume.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        trackVolume.Location = new Point(2294, 39);
        trackVolume.Maximum = 100;
        trackVolume.Name = "trackVolume";
        trackVolume.Size = new Size(135, 45);
        trackVolume.TabIndex = 4;
        trackVolume.TickStyle = TickStyle.None;
        trackVolume.Value = 80;
        // 
        // lblVolume
        // 
        lblVolume.AutoSize = true;
        lblVolume.ForeColor = Color.FromArgb(152, 176, 195);
        lblVolume.Location = new Point(1048, 12);
        lblVolume.Name = "lblVolume";
        lblVolume.Size = new Size(54, 15);
        lblVolume.TabIndex = 5;
        lblVolume.Text = "VOLUME";
        // 
        // panelTimeline
        // 
        panelTimeline.Controls.Add(timelineLayout);
        panelTimeline.Dock = DockStyle.Fill;
        panelTimeline.Location = new Point(12, 225);
        panelTimeline.Margin = new Padding(0, 0, 0, 8);
        panelTimeline.Name = "panelTimeline";
        panelTimeline.Size = new Size(1456, 173);
        panelTimeline.TabIndex = 3;
        // 
        // timelineLayout
        // 
        timelineLayout.ColumnCount = 1;
        timelineLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        timelineLayout.Controls.Add(timelineHeader, 0, 0);
        timelineLayout.Controls.Add(timeline, 0, 1);
        timelineLayout.Controls.Add(scrollTimeline, 0, 2);
        timelineLayout.Dock = DockStyle.Fill;
        timelineLayout.Location = new Point(0, 0);
        timelineLayout.Margin = new Padding(0);
        timelineLayout.Name = "timelineLayout";
        timelineLayout.RowCount = 3;
        timelineLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        timelineLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        timelineLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));
        timelineLayout.Size = new Size(1456, 173);
        timelineLayout.TabIndex = 0;
        // 
        // timelineHeader
        // 
        timelineHeader.Controls.Add(lblTimeline);
        timelineHeader.Dock = DockStyle.Fill;
        timelineHeader.Location = new Point(0, 0);
        timelineHeader.Margin = new Padding(0);
        timelineHeader.Name = "timelineHeader";
        timelineHeader.Size = new Size(1456, 32);
        timelineHeader.TabIndex = 0;
        // 
        // lblTimeline
        // 
        lblTimeline.AutoSize = true;
        lblTimeline.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblTimeline.ForeColor = Color.FromArgb(144, 165, 190);
        lblTimeline.Location = new Point(1, 6);
        lblTimeline.Name = "lblTimeline";
        lblTimeline.Size = new Size(278, 19);
        lblTimeline.TabIndex = 0;
        lblTimeline.Text = "CIFRAS POR TEMPO  |  clique para saltar";
        // 
        // timeline
        // 
        timeline.BackColor = Color.FromArgb(17, 27, 41);
        timeline.Dock = DockStyle.Fill;
        timeline.Font = new Font("Segoe UI", 9F);
        timeline.ForeColor = Color.FromArgb(180, 204, 218);
        timeline.Location = new Point(0, 32);
        timeline.Margin = new Padding(0);
        timeline.MinimumSize = new Size(120, 70);
        timeline.Name = "timeline";
        timeline.Size = new Size(1456, 141);
        timeline.TabIndex = 1;
        // 
        // scrollTimeline
        // 
        scrollTimeline.Dock = DockStyle.Fill;
        scrollTimeline.Enabled = false;
        scrollTimeline.LargeChange = 30;
        scrollTimeline.Location = new Point(0, 173);
        scrollTimeline.Maximum = 29;
        scrollTimeline.Name = "scrollTimeline";
        scrollTimeline.Size = new Size(1456, 1);
        scrollTimeline.SmallChange = 3;
        scrollTimeline.TabIndex = 2;
        scrollTimeline.Visible = false;
        // 
        // centerLayout
        // 
        centerLayout.ColumnCount = 2;
        centerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 69F));
        centerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31F));
        centerLayout.Controls.Add(leftCenterLayout, 0, 0);
        centerLayout.Controls.Add(fretboard, 1, 0);
        centerLayout.Dock = DockStyle.Fill;
        centerLayout.Location = new Point(12, 406);
        centerLayout.Margin = new Padding(0);
        centerLayout.Name = "centerLayout";
        centerLayout.RowCount = 1;
        centerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        centerLayout.Size = new Size(1456, 455);
        centerLayout.TabIndex = 4;
        // 
        // leftCenterLayout
        // 
        leftCenterLayout.ColumnCount = 1;
        leftCenterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        leftCenterLayout.Controls.Add(panelNow, 0, 0);
        leftCenterLayout.Controls.Add(panelGrid, 0, 1);
        leftCenterLayout.Dock = DockStyle.Fill;
        leftCenterLayout.Location = new Point(0, 0);
        leftCenterLayout.Margin = new Padding(0);
        leftCenterLayout.Name = "leftCenterLayout";
        leftCenterLayout.RowCount = 2;
        leftCenterLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 202F));
        leftCenterLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        leftCenterLayout.Size = new Size(1004, 455);
        leftCenterLayout.TabIndex = 0;
        // 
        // panelNow
        // 
        panelNow.BackColor = Color.FromArgb(23, 35, 52);
        panelNow.Controls.Add(lblNowTitle);
        panelNow.Controls.Add(lblCurrent);
        panelNow.Controls.Add(lblQuality);
        panelNow.Controls.Add(lblTriad);
        panelNow.Controls.Add(lblShape);
        panelNow.Controls.Add(lblNext);
        panelNow.Controls.Add(lblBass);
        panelNow.Controls.Add(lblEvidence);
        panelNow.Dock = DockStyle.Fill;
        panelNow.Location = new Point(0, 0);
        panelNow.Margin = new Padding(0, 0, 8, 7);
        panelNow.Name = "panelNow";
        panelNow.Size = new Size(996, 195);
        panelNow.TabIndex = 0;
        // 
        // lblNowTitle
        // 
        lblNowTitle.AutoSize = true;
        lblNowTitle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblNowTitle.ForeColor = Color.FromArgb(139, 164, 191);
        lblNowTitle.Location = new Point(18, 10);
        lblNowTitle.Name = "lblNowTitle";
        lblNowTitle.Size = new Size(230, 19);
        lblNowTitle.TabIndex = 0;
        lblNowTitle.Text = "IDENTIDADE HARMÔNICA ATUAL";
        // 
        // lblCurrent
        // 
        lblCurrent.AutoSize = true;
        lblCurrent.Font = new Font("Segoe UI", 59F, FontStyle.Bold);
        lblCurrent.ForeColor = Color.FromArgb(110, 226, 206);
        lblCurrent.Location = new Point(12, 30);
        lblCurrent.Name = "lblCurrent";
        lblCurrent.Size = new Size(109, 105);
        lblCurrent.TabIndex = 1;
        lblCurrent.Text = "--";
        // 
        // lblQuality
        // 
        lblQuality.AutoSize = true;
        lblQuality.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        lblQuality.ForeColor = Color.FromArgb(247, 197, 127);
        lblQuality.Location = new Point(252, 69);
        lblQuality.Name = "lblQuality";
        lblQuality.Size = new Size(31, 30);
        lblQuality.TabIndex = 2;
        lblQuality.Text = "--";
        // 
        // lblTriad
        // 
        lblTriad.AutoSize = true;
        lblTriad.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblTriad.ForeColor = Color.White;
        lblTriad.Location = new Point(20, 137);
        lblTriad.Name = "lblTriad";
        lblTriad.Size = new Size(83, 20);
        lblTriad.TabIndex = 3;
        lblTriad.Text = "TRÍADE  --";
        // 
        // lblShape
        // 
        lblShape.AutoSize = true;
        lblShape.ForeColor = Color.FromArgb(145, 223, 201);
        lblShape.Location = new Point(21, 163);
        lblShape.Name = "lblShape";
        lblShape.Size = new Size(59, 15);
        lblShape.TabIndex = 4;
        lblShape.Text = "CASAS  --";
        // 
        // lblNext
        // 
        lblNext.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        lblNext.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblNext.ForeColor = Color.FromArgb(223, 234, 246);
        lblNext.Location = new Point(1386, 27);
        lblNext.Name = "lblNext";
        lblNext.Size = new Size(315, 32);
        lblNext.TabIndex = 5;
        lblNext.Text = "PRÓXIMA  --";
        lblNext.TextAlign = ContentAlignment.MiddleRight;
        // 
        // lblBass
        // 
        lblBass.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        lblBass.ForeColor = Color.FromArgb(162, 181, 205);
        lblBass.Location = new Point(1386, 72);
        lblBass.Name = "lblBass";
        lblBass.Size = new Size(315, 30);
        lblBass.TabIndex = 6;
        lblBass.Text = "FUNDAMENTAL  --";
        lblBass.TextAlign = ContentAlignment.MiddleRight;
        // 
        // lblEvidence
        // 
        lblEvidence.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        lblEvidence.ForeColor = Color.FromArgb(131, 154, 180);
        lblEvidence.Location = new Point(1386, 116);
        lblEvidence.Name = "lblEvidence";
        lblEvidence.Size = new Size(315, 27);
        lblEvidence.TabIndex = 7;
        lblEvidence.Text = "MOTOR  --";
        lblEvidence.TextAlign = ContentAlignment.MiddleRight;
        // 
        // panelGrid
        // 
        panelGrid.Controls.Add(lblGrid);
        panelGrid.Controls.Add(gridChords);
        panelGrid.Dock = DockStyle.Fill;
        panelGrid.Location = new Point(0, 202);
        panelGrid.Margin = new Padding(0, 0, 8, 0);
        panelGrid.Name = "panelGrid";
        panelGrid.Size = new Size(996, 253);
        panelGrid.TabIndex = 1;
        // 
        // lblGrid
        // 
        lblGrid.AutoSize = true;
        lblGrid.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblGrid.ForeColor = Color.FromArgb(144, 165, 190);
        lblGrid.Location = new Point(2, 1);
        lblGrid.Name = "lblGrid";
        lblGrid.Size = new Size(478, 19);
        lblGrid.TabIndex = 0;
        lblGrid.Text = "CIFRAS POR FRASE  |  duplo clique: tocar  |  edite TRÍADE para corrigir";
        // 
        // gridChords
        // 
        gridChords.AllowUserToAddRows = false;
        gridChords.AllowUserToDeleteRows = false;
        gridChords.AllowUserToResizeRows = false;
        gridChords.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        gridChords.BackgroundColor = Color.FromArgb(18, 27, 40);
        gridChords.BorderStyle = BorderStyle.None;
        dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle1.BackColor = Color.FromArgb(31, 47, 65);
        dataGridViewCellStyle1.Font = new Font("Segoe UI", 9F);
        dataGridViewCellStyle1.ForeColor = Color.White;
        dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
        dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
        dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
        gridChords.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
        gridChords.ColumnHeadersHeight = 33;
        gridChords.Columns.AddRange(new DataGridViewColumn[] { colStart, colEnd, colChord, colConfidence });
        dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle2.BackColor = Color.FromArgb(19, 28, 42);
        dataGridViewCellStyle2.Font = new Font("Segoe UI", 9F);
        dataGridViewCellStyle2.ForeColor = Color.FromArgb(233, 238, 243);
        dataGridViewCellStyle2.SelectionBackColor = Color.FromArgb(43, 93, 98);
        dataGridViewCellStyle2.SelectionForeColor = Color.White;
        dataGridViewCellStyle2.WrapMode = DataGridViewTriState.False;
        gridChords.DefaultCellStyle = dataGridViewCellStyle2;
        gridChords.Dock = DockStyle.Bottom;
        gridChords.EnableHeadersVisualStyles = false;
        gridChords.GridColor = Color.FromArgb(45, 57, 73);
        gridChords.Location = new Point(0, 113);
        gridChords.MultiSelect = false;
        gridChords.Name = "gridChords";
        gridChords.RowHeadersVisible = false;
        gridChords.RowTemplate.Height = 28;
        gridChords.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        gridChords.Size = new Size(996, 140);
        gridChords.TabIndex = 1;
        // 
        // colStart
        // 
        colStart.FillWeight = 18F;
        colStart.HeaderText = "INÍCIO";
        colStart.Name = "colStart";
        colStart.ReadOnly = true;
        // 
        // colEnd
        // 
        colEnd.FillWeight = 18F;
        colEnd.HeaderText = "FIM";
        colEnd.Name = "colEnd";
        colEnd.ReadOnly = true;
        // 
        // colChord
        // 
        colChord.FillWeight = 30F;
        colChord.HeaderText = "TRÍADE";
        colChord.Name = "colChord";
        // 
        // colConfidence
        // 
        colConfidence.FillWeight = 23F;
        colConfidence.HeaderText = "CHORDINO BRUTO";
        colConfidence.Name = "colConfidence";
        colConfidence.ReadOnly = true;
        // 
        // fretboard
        // 
        fretboard.AccessibleName = "Braco do contrabaixo vertical, cordas E A D G; fundamental e triade sugerida.";
        fretboard.BackColor = Color.FromArgb(17, 26, 39);
        fretboard.Dock = DockStyle.Fill;
        fretboard.Font = new Font("Segoe UI", 9F);
        fretboard.ForeColor = Color.FromArgb(235, 241, 250);
        fretboard.Location = new Point(1006, 0);
        fretboard.Margin = new Padding(2, 0, 0, 0);
        fretboard.MinimumSize = new Size(200, 315);
        fretboard.Name = "fretboard";
        fretboard.Size = new Size(450, 455);
        fretboard.TabIndex = 1;
        // 
        // footer
        // 
        footer.Controls.Add(lblStatus);
        footer.Controls.Add(progressAnalysis);
        footer.Dock = DockStyle.Fill;
        footer.Location = new Point(15, 864);
        footer.Name = "footer";
        footer.Size = new Size(1450, 26);
        footer.TabIndex = 5;
        // 
        // lblStatus
        // 
        lblStatus.Dock = DockStyle.Fill;
        lblStatus.ForeColor = Color.FromArgb(146, 172, 192);
        lblStatus.Location = new Point(0, 0);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(1450, 26);
        lblStatus.TabIndex = 0;
        lblStatus.Text = "Chordino/Vamp incluídos localmente; pronto para abrir sua música.";
        lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // progressAnalysis
        // 
        progressAnalysis.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        progressAnalysis.Location = new Point(2370, 9);
        progressAnalysis.Name = "progressAnalysis";
        progressAnalysis.Size = new Size(188, 12);
        progressAnalysis.TabIndex = 1;
        // 
        // numSync
        // 
        numSync.Increment = new decimal(new int[] { 10, 0, 0, 0 });
        numSync.Location = new Point(0, 0);
        numSync.Maximum = new decimal(new int[] { 600, 0, 0, 0 });
        numSync.Minimum = new decimal(new int[] { 600, 0, 0, int.MinValue });
        numSync.Name = "numSync";
        numSync.Size = new Size(120, 23);
        numSync.TabIndex = 0;
        numSync.Visible = false;
        // 
        // lblSync
        // 
        lblSync.Location = new Point(0, 0);
        lblSync.Name = "lblSync";
        lblSync.Size = new Size(100, 23);
        lblSync.TabIndex = 0;
        lblSync.Visible = false;
        // 
        // lblTimelineZoom
        // 
        lblTimelineZoom.Location = new Point(0, 0);
        lblTimelineZoom.Name = "lblTimelineZoom";
        lblTimelineZoom.Size = new Size(100, 23);
        lblTimelineZoom.TabIndex = 0;
        lblTimelineZoom.Text = "JANELA (s)";
        lblTimelineZoom.Visible = false;
        // 
        // numTimelineZoom
        // 
        numTimelineZoom.Increment = new decimal(new int[] { 15, 0, 0, 0 });
        numTimelineZoom.Location = new Point(0, 0);
        numTimelineZoom.Maximum = new decimal(new int[] { 180, 0, 0, 0 });
        numTimelineZoom.Minimum = new decimal(new int[] { 15, 0, 0, 0 });
        numTimelineZoom.Name = "numTimelineZoom";
        numTimelineZoom.Size = new Size(120, 23);
        numTimelineZoom.TabIndex = 0;
        numTimelineZoom.Value = new decimal(new int[] { 30, 0, 0, 0 });
        numTimelineZoom.Visible = false;
        // 
        // btnTimelineFollow
        // 
        btnTimelineFollow.Location = new Point(0, 0);
        btnTimelineFollow.Name = "btnTimelineFollow";
        btnTimelineFollow.Size = new Size(75, 23);
        btnTimelineFollow.TabIndex = 0;
        btnTimelineFollow.Text = "SEGUIR: ON";
        btnTimelineFollow.Visible = false;
        // 
        // timerPlayhead
        // 
        timerPlayhead.Enabled = true;
        timerPlayhead.Interval = 70;
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(14, 19, 29);
        ClientSize = new Size(1480, 900);
        Controls.Add(rootLayout);
        Font = new Font("Segoe UI", 9F);
        ForeColor = Color.FromArgb(232, 238, 247);
        Icon = (Icon)resources.GetObject("$this.Icon");
        MinimumSize = new Size(1050, 730);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Cadelão BASS";
        WindowState = FormWindowState.Maximized;
        rootLayout.ResumeLayout(false);
        headerPanel.ResumeLayout(false);
        headerPanel.PerformLayout();
        toolbar.ResumeLayout(false);
        panelTransport.ResumeLayout(false);
        panelTransport.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)trackSeek).EndInit();
        ((System.ComponentModel.ISupportInitialize)trackVolume).EndInit();
        panelTimeline.ResumeLayout(false);
        timelineLayout.ResumeLayout(false);
        timelineHeader.ResumeLayout(false);
        timelineHeader.PerformLayout();
        centerLayout.ResumeLayout(false);
        leftCenterLayout.ResumeLayout(false);
        panelNow.ResumeLayout(false);
        panelNow.PerformLayout();
        panelGrid.ResumeLayout(false);
        panelGrid.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)gridChords).EndInit();
        footer.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)numSync).EndInit();
        ((System.ComponentModel.ISupportInitialize)numTimelineZoom).EndInit();
        ResumeLayout(false);
    }
}
