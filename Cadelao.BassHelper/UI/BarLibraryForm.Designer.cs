namespace Cadelao.BassHelper.UI;

partial class BarLibraryForm
{
    private System.ComponentModel.IContainer? components = null;
    private TableLayoutPanel layout = null!;
    private Panel header = null!;
    private Label lblTitle = null!;
    private Label lblHint = null!;
    private Button btnRefresh = null!;
    private SplitContainer split = null!;
    private Panel leftHeader = null!;
    private Label lblAlbums = null!;
    private TextBox txtFilter = null!;
    private ListView listAlbums = null!;
    private DataGridView gridSongs = null!;
    private DataGridViewTextBoxColumn colTrack = null!;
    private DataGridViewTextBoxColumn colTitle = null!;
    private DataGridViewTextBoxColumn colArtist = null!;
    private DataGridViewTextBoxColumn colAlbum = null!;
    private DataGridViewTextBoxColumn colDuration = null!;
    private DataGridViewTextBoxColumn colRating = null!;
    private Panel footer = null!;
    private Label lblStatus = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopLibraryTasks();
            components?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        layout = new TableLayoutPanel();
        header = new Panel();
        lblTitle = new Label();
        lblHint = new Label();
        btnRefresh = new Button();
        split = new SplitContainer();
        leftHeader = new Panel();
        lblAlbums = new Label();
        txtFilter = new TextBox();
        listAlbums = new ListView();
        gridSongs = new DataGridView();
        colTrack = new DataGridViewTextBoxColumn();
        colTitle = new DataGridViewTextBoxColumn();
        colArtist = new DataGridViewTextBoxColumn();
        colAlbum = new DataGridViewTextBoxColumn();
        colDuration = new DataGridViewTextBoxColumn();
        colRating = new DataGridViewTextBoxColumn();
        footer = new Panel();
        lblStatus = new Label();

        layout.SuspendLayout();
        header.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)split).BeginInit();
        split.Panel1.SuspendLayout();
        split.Panel2.SuspendLayout();
        split.SuspendLayout();
        leftHeader.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)gridSongs).BeginInit();
        footer.SuspendLayout();
        SuspendLayout();

        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(14, 19, 29);
        ForeColor = Color.FromArgb(232, 238, 247);
        Font = new Font("Segoe UI", 9F);
        ClientSize = new Size(1160, 695);
        MinimumSize = new Size(830, 490);
        StartPosition = FormStartPosition.CenterParent;
        Name = "BarLibraryForm";
        Text = "BREGA BASS | Biblioteca CadelaoBar8800 (somente leitura)";

        layout.Dock = DockStyle.Fill;
        layout.ColumnCount = 1;
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowCount = 3;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        layout.Padding = new Padding(10, 6, 10, 7);
        Controls.Add(layout);

        header.Size = new Size(1140, 76);
        header.Dock = DockStyle.Fill;
        header.BackColor = Color.FromArgb(24, 34, 47);
        lblTitle.Text = "BIBLIOTECA CADELAO BAR  ·  SOMENTE LEITURA";
        lblTitle.Name = "lblTitle";
        lblTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        lblTitle.ForeColor = Color.FromArgb(110, 226, 206);
        lblTitle.AutoSize = true;
        lblTitle.Location = new Point(15, 8);
        lblHint.Name = "lblHint";
        lblHint.Text = "Duplo clique na faixa: tocar e analisar no BREGA BASS. As estrelas nao podem ser alteradas aqui.";
        lblHint.AutoSize = true;
        lblHint.ForeColor = Color.FromArgb(151, 177, 198);
        lblHint.Location = new Point(17, 46);
        btnRefresh.Name = "btnRefresh";
        btnRefresh.Text = "ATUALIZAR";
        btnRefresh.BackColor = Color.FromArgb(35, 90, 105);
        btnRefresh.ForeColor = Color.White;
        btnRefresh.FlatStyle = FlatStyle.Flat;
        btnRefresh.FlatAppearance.BorderSize = 0;
        btnRefresh.Size = new Size(133, 36);
        btnRefresh.Location = new Point(990, 18);
        btnRefresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        header.Controls.AddRange(new Control[] { lblTitle, lblHint, btnRefresh });
        layout.Controls.Add(header, 0, 0);

        split.Name = "split";
        split.Size = new Size(1140, 560);
        split.Dock = DockStyle.Fill;
        split.BackColor = Color.FromArgb(34, 50, 65);
        split.SplitterWidth = 5;
        split.SplitterDistance = 355;
        split.Panel1MinSize = 210;
        split.Panel2MinSize = 370;
        split.Margin = new Padding(0, 6, 0, 0);

        leftHeader.Name = "leftHeader";
        leftHeader.Size = new Size(355, 73);
        leftHeader.Dock = DockStyle.Top;
        leftHeader.Height = 73;
        leftHeader.BackColor = Color.FromArgb(20, 29, 41);
        lblAlbums.Name = "lblAlbums";
        lblAlbums.Text = "ARTISTA / ALBUM";
        lblAlbums.ForeColor = Color.FromArgb(117, 199, 229);
        lblAlbums.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblAlbums.AutoSize = true;
        lblAlbums.Location = new Point(8, 8);
        txtFilter.Name = "txtFilter";
        txtFilter.PlaceholderText = "Filtrar artistas ou albuns...";
        txtFilter.BorderStyle = BorderStyle.FixedSingle;
        txtFilter.BackColor = Color.FromArgb(28, 40, 55);
        txtFilter.ForeColor = Color.White;
        txtFilter.Location = new Point(8, 35);
        txtFilter.Size = new Size(330, 27);
        txtFilter.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        leftHeader.Controls.AddRange(new Control[] { lblAlbums, txtFilter });

        listAlbums.Name = "listAlbums";
        listAlbums.Dock = DockStyle.Fill;
        listAlbums.VirtualMode = true;
        listAlbums.View = View.Details;
        listAlbums.Columns.Add("Album", 440);
        listAlbums.HeaderStyle = ColumnHeaderStyle.None;
        listAlbums.FullRowSelect = true;
        listAlbums.HideSelection = false;
        listAlbums.MultiSelect = false;
        listAlbums.BorderStyle = BorderStyle.None;
        listAlbums.BackColor = Color.FromArgb(19, 27, 39);
        listAlbums.ForeColor = Color.FromArgb(221, 230, 242);
        listAlbums.Font = new Font("Segoe UI", 9F);
        split.Panel1.Controls.Add(listAlbums);
        split.Panel1.Controls.Add(leftHeader);

        gridSongs.Name = "gridSongs";
        gridSongs.Dock = DockStyle.Fill;
        gridSongs.VirtualMode = true;
        gridSongs.ReadOnly = true;
        gridSongs.AllowUserToAddRows = false;
        gridSongs.AllowUserToDeleteRows = false;
        gridSongs.AllowUserToResizeRows = false;
        gridSongs.MultiSelect = true;
        gridSongs.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        gridSongs.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        gridSongs.RowHeadersVisible = false;
        gridSongs.BackgroundColor = Color.FromArgb(19, 27, 39);
        gridSongs.BorderStyle = BorderStyle.None;
        gridSongs.GridColor = Color.FromArgb(35, 47, 61);
        gridSongs.EnableHeadersVisualStyles = false;
        gridSongs.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(33, 49, 65);
        gridSongs.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(157, 217, 236);
        gridSongs.DefaultCellStyle.BackColor = Color.FromArgb(20, 29, 40);
        gridSongs.DefaultCellStyle.ForeColor = Color.FromArgb(226, 236, 244);
        gridSongs.DefaultCellStyle.SelectionBackColor = Color.FromArgb(38, 90, 116);
        gridSongs.DefaultCellStyle.SelectionForeColor = Color.White;
        gridSongs.RowTemplate.Height = 27;
        gridSongs.ColumnHeadersHeight = 34;
        gridSongs.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        colTrack.Name = "colTrack";
        colTrack.HeaderText = "#";
        colTrack.FillWeight = 13;
        colTrack.SortMode = DataGridViewColumnSortMode.NotSortable;
        colTitle.Name = "colTitle";
        colTitle.HeaderText = "Musica";
        colTitle.FillWeight = 100;
        colTitle.SortMode = DataGridViewColumnSortMode.NotSortable;
        colArtist.Name = "colArtist";
        colArtist.HeaderText = "Artista";
        colArtist.FillWeight = 55;
        colArtist.SortMode = DataGridViewColumnSortMode.NotSortable;
        colAlbum.Name = "colAlbum";
        colAlbum.HeaderText = "Album";
        colAlbum.FillWeight = 50;
        colAlbum.SortMode = DataGridViewColumnSortMode.NotSortable;
        colDuration.Name = "colDuration";
        colDuration.HeaderText = "Tempo";
        colDuration.FillWeight = 23;
        colDuration.SortMode = DataGridViewColumnSortMode.NotSortable;
        colRating.Name = "colRating";
        colRating.HeaderText = "Estrelas";
        colRating.FillWeight = 30;
        colRating.SortMode = DataGridViewColumnSortMode.NotSortable;
        colRating.DefaultCellStyle.ForeColor = Color.FromArgb(245, 192, 65);
        colRating.DefaultCellStyle.SelectionForeColor = Color.FromArgb(255, 221, 125);
        gridSongs.Columns.AddRange(new DataGridViewColumn[]
        {
            colTrack, colTitle, colArtist, colAlbum, colDuration, colRating
        });
        split.Panel2.Controls.Add(gridSongs);
        layout.Controls.Add(split, 0, 1);

        footer.Name = "footer";
        footer.Dock = DockStyle.Fill;
        lblStatus.Name = "lblStatus";
        lblStatus.Dock = DockStyle.Fill;
        lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        lblStatus.ForeColor = Color.FromArgb(147, 173, 195);
        lblStatus.Text = "Aguardando leitura do catalogo...";
        footer.Controls.Add(lblStatus);
        layout.Controls.Add(footer, 0, 2);

        footer.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)gridSongs).EndInit();
        leftHeader.ResumeLayout(false);
        leftHeader.PerformLayout();
        split.Panel1.ResumeLayout(false);
        split.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)split).EndInit();
        split.ResumeLayout(false);
        header.ResumeLayout(false);
        header.PerformLayout();
        layout.ResumeLayout(false);
        ResumeLayout(false);
    }
}
