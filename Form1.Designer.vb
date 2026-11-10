<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Form1
    Inherits System.Windows.Forms.Form

    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then components.Dispose()
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    Private components As System.ComponentModel.IContainer

    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Dim resources As New System.ComponentModel.ComponentResourceManager(GetType(Form1))
        shell = New TableLayoutPanel()
        brandPanel = New TableLayoutPanel()
        logoPicture = New PictureBox()
        companyLabel = New Label()
        taglineLabel = New Label()
        journeyLabel = New Label()
        stationsLabel = New Label()
        accentBar = New Panel()
        brandFooter = New Label()
        loginHost = New Panel()
        loginCard = New Panel()
        loginTable = New TableLayoutPanel()
        welcomeLabel = New Label()
        subtitleLabel = New Label()
        usernameLabel = New Label()
        usernameBox = New TextBox()
        passwordLabel = New Label()
        passwordBox = New TextBox()
        signInButton = New Button()
        registerButton = New Button()
        demoLabel = New Label()
        presentationCheck = New CheckBox()
        SuspendLayout()
        '
        ' shell
        '
        shell.ColumnCount = 2
        shell.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 45.0F))
        shell.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 55.0F))
        shell.RowCount = 1
        shell.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        shell.Location = New Point(24, 24)
        shell.Size = New Size(1072, 712)
        shell.Name = "shell"
        '
        ' brandPanel
        '
        brandPanel.BackColor = Color.FromArgb(16, 44, 70)
        brandPanel.Dock = DockStyle.Fill
        brandPanel.ColumnCount = 1
        brandPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        brandPanel.RowCount = 7
        brandPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 30.0F))
        brandPanel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        brandPanel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        brandPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 32.0F))
        brandPanel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        brandPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 12.0F))
        brandPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 18.0F))
        brandPanel.Padding = New Padding(30, 26, 26, 22)
        brandPanel.Margin = New Padding(0, 0, 12, 0)
        brandPanel.Name = "brandPanel"
        '
        ' logoPicture
        '
        logoPicture.BackColor = Color.White
        logoPicture.Image = CType(resources.GetObject("logoPicture.Image"), Image)
        logoPicture.Size = New Size(128, 128)
        logoPicture.SizeMode = PictureBoxSizeMode.Zoom
        logoPicture.Margin = New Padding(0, 0, 0, 20)
        logoPicture.TabStop = False
        logoPicture.AccessibleName = "Group1 Transport bus and road logo"
        logoPicture.Name = "logoPicture"
        '
        ' companyLabel
        '
        companyLabel.AutoSize = True
        companyLabel.Dock = DockStyle.Top
        companyLabel.Text = "GROUP1" & vbCrLf & "TRANSPORT"
        companyLabel.Font = New Font("Segoe UI", 28.0F, FontStyle.Bold)
        companyLabel.ForeColor = Color.White
        companyLabel.Margin = New Padding(0, 4, 0, 12)
        companyLabel.Name = "companyLabel"
        '
        ' taglineLabel
        '
        taglineLabel.AutoSize = True
        taglineLabel.Dock = DockStyle.Top
        taglineLabel.Text = "Your journey." & vbCrLf & "One ticket away."
        taglineLabel.Font = New Font("Segoe UI", 20.0F, FontStyle.Bold)
        taglineLabel.ForeColor = Color.FromArgb(74, 225, 193)
        taglineLabel.Margin = New Padding(0, 4, 0, 12)
        taglineLabel.Name = "taglineLabel"
        '
        ' journeyLabel
        '
        journeyLabel.AutoSize = True
        journeyLabel.Dock = DockStyle.Top
        journeyLabel.Text = "Digital tickets. Live seats." & vbCrLf & "Travel with confidence."
        journeyLabel.Font = New Font("Segoe UI", 14.0F, FontStyle.Regular)
        journeyLabel.ForeColor = Color.White
        journeyLabel.Margin = New Padding(0, 4, 0, 12)
        journeyLabel.Name = "journeyLabel"
        '
        ' stationsLabel
        '
        stationsLabel.AutoSize = True
        stationsLabel.Dock = DockStyle.Top
        stationsLabel.Text = "ACCRA / KUMASI / TAMALE" & vbCrLf & "TAKORADI / CAPE COAST"
        stationsLabel.Font = New Font("Segoe UI", 12.0F, FontStyle.Bold)
        stationsLabel.ForeColor = Color.FromArgb(250, 196, 74)
        stationsLabel.Margin = New Padding(0, 4, 0, 12)
        stationsLabel.Name = "stationsLabel"
        '
        ' accentBar
        '
        accentBar.BackColor = Color.FromArgb(0, 169, 143)
        accentBar.Dock = DockStyle.Top
        accentBar.Height = 5
        accentBar.Margin = New Padding(0)
        accentBar.Name = "accentBar"
        '
        ' brandFooter
        '
        brandFooter.AutoSize = True
        brandFooter.Dock = DockStyle.Bottom
        brandFooter.Text = "CLASSROOM DEMO" & vbCrLf & "Payments & SMS are simulated."
        brandFooter.Font = New Font("Segoe UI", 11.0F, FontStyle.Regular)
        brandFooter.ForeColor = Color.White
        brandFooter.Margin = New Padding(0, 4, 0, 12)
        brandFooter.Name = "brandFooter"
        '
        ' loginHost
        '
        loginHost.Dock = DockStyle.Fill
        loginHost.BackColor = Color.FromArgb(230, 245, 243)
        loginHost.AutoScroll = True
        loginHost.Margin = New Padding(0)
        loginHost.Name = "loginHost"
        '
        ' loginCard
        '
        loginCard.BackColor = Color.White
        loginCard.Location = New Point(25, 34)
        loginCard.Size = New Size(530, 644)
        loginCard.Padding = New Padding(28)
        loginCard.Name = "loginCard"
        '
        ' loginTable
        '
        loginTable.AutoSize = True
        loginTable.AutoSizeMode = AutoSizeMode.GrowAndShrink
        loginTable.Dock = DockStyle.Top
        loginTable.ColumnCount = 1
        loginTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        loginTable.RowCount = 10
        loginTable.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        loginTable.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        loginTable.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        loginTable.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        loginTable.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        loginTable.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        loginTable.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        loginTable.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        loginTable.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        loginTable.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        loginTable.Name = "loginTable"
        '
        ' welcomeLabel
        '
        welcomeLabel.AutoSize = True
        welcomeLabel.Dock = DockStyle.Top
        welcomeLabel.Text = "Welcome aboard"
        welcomeLabel.Font = New Font("Segoe UI", 28.0F, FontStyle.Bold)
        welcomeLabel.ForeColor = Color.FromArgb(16, 44, 70)
        welcomeLabel.Margin = New Padding(0, 0, 0, 6)
        welcomeLabel.Name = "welcomeLabel"
        '
        ' subtitleLabel
        '
        subtitleLabel.AutoSize = True
        subtitleLabel.Dock = DockStyle.Top
        subtitleLabel.Text = "Sign in. Find your next journey."
        subtitleLabel.Font = New Font("Segoe UI", 14.0F, FontStyle.Regular)
        subtitleLabel.ForeColor = Color.FromArgb(57, 77, 96)
        subtitleLabel.Margin = New Padding(0, 0, 0, 24)
        subtitleLabel.Name = "subtitleLabel"
        '
        ' usernameLabel
        '
        usernameLabel.AutoSize = True
        usernameLabel.Dock = DockStyle.Top
        usernameLabel.Text = "Username"
        usernameLabel.Font = New Font("Segoe UI", 14.0F, FontStyle.Bold)
        usernameLabel.ForeColor = Color.FromArgb(16, 44, 70)
        usernameLabel.Margin = New Padding(0, 0, 0, 6)
        usernameLabel.Name = "usernameLabel"
        '
        ' usernameBox
        '
        usernameBox.Dock = DockStyle.Top
        usernameBox.Font = New Font("Segoe UI", 18.0F)
        usernameBox.Margin = New Padding(0, 0, 0, 16)
        usernameBox.MaxLength = 30
        usernameBox.PlaceholderText = "Enter your username"
        usernameBox.TabIndex = 0
        usernameBox.Name = "usernameBox"
        '
        ' passwordLabel
        '
        passwordLabel.AutoSize = True
        passwordLabel.Dock = DockStyle.Top
        passwordLabel.Text = "Password"
        passwordLabel.Font = New Font("Segoe UI", 14.0F, FontStyle.Bold)
        passwordLabel.ForeColor = Color.FromArgb(16, 44, 70)
        passwordLabel.Margin = New Padding(0, 0, 0, 6)
        passwordLabel.Name = "passwordLabel"
        '
        ' passwordBox
        '
        passwordBox.Dock = DockStyle.Top
        passwordBox.Font = New Font("Segoe UI", 18.0F)
        passwordBox.Margin = New Padding(0, 0, 0, 24)
        passwordBox.MaxLength = 128
        passwordBox.PlaceholderText = "Enter your password"
        passwordBox.UseSystemPasswordChar = True
        passwordBox.TabIndex = 1
        passwordBox.Name = "passwordBox"
        '
        ' signInButton
        '
        signInButton.BackColor = Color.FromArgb(21, 101, 192)
        signInButton.Dock = DockStyle.Top
        signInButton.FlatAppearance.BorderSize = 0
        signInButton.FlatStyle = FlatStyle.Flat
        signInButton.Font = New Font("Segoe UI", 16.0F, FontStyle.Bold)
        signInButton.ForeColor = Color.White
        signInButton.Size = New Size(474, 52)
        signInButton.Text = "Sign in  →"
        signInButton.Margin = New Padding(0, 0, 0, 12)
        signInButton.Cursor = Cursors.Hand
        signInButton.TabIndex = 2
        signInButton.Name = "signInButton"
        '
        ' registerButton
        '
        registerButton.BackColor = Color.FromArgb(0, 124, 108)
        registerButton.Dock = DockStyle.Top
        registerButton.FlatAppearance.BorderSize = 0
        registerButton.FlatStyle = FlatStyle.Flat
        registerButton.Font = New Font("Segoe UI", 14.0F, FontStyle.Bold)
        registerButton.ForeColor = Color.White
        registerButton.Size = New Size(474, 48)
        registerButton.Text = "Create a passenger account"
        registerButton.Margin = New Padding(0, 0, 0, 18)
        registerButton.Cursor = Cursors.Hand
        registerButton.TabIndex = 3
        registerButton.Name = "registerButton"
        '
        ' demoLabel
        '
        demoLabel.AutoSize = True
        demoLabel.Dock = DockStyle.Top
        demoLabel.Text = "TRY THE DEMO" & vbCrLf & "admin · bookman · passenger" & vbCrLf & "Password: Demo@123"
        demoLabel.Font = New Font("Segoe UI", 12.0F, FontStyle.Regular)
        demoLabel.ForeColor = Color.FromArgb(16, 44, 70)
        demoLabel.Margin = New Padding(0, 0, 0, 12)
        demoLabel.BackColor = Color.FromArgb(239, 245, 252)
        demoLabel.Padding = New Padding(12)
        demoLabel.Name = "demoLabel"
        '
        ' presentationCheck
        '
        presentationCheck.AutoSize = True
        presentationCheck.Font = New Font("Segoe UI", 12.0F, FontStyle.Bold)
        presentationCheck.Text = "Presentation mode · larger text"
        presentationCheck.Margin = New Padding(0)
        presentationCheck.TabIndex = 4
        presentationCheck.Name = "presentationCheck"
        shell.Controls.Add(brandPanel, 0, 0)
        shell.Controls.Add(loginHost, 1, 0)
        loginHost.Controls.Add(loginCard)
        loginCard.Controls.Add(loginTable)
        brandPanel.Controls.Add(logoPicture, 0, 0)
        brandPanel.Controls.Add(companyLabel, 0, 1)
        brandPanel.Controls.Add(taglineLabel, 0, 2)
        brandPanel.Controls.Add(journeyLabel, 0, 3)
        brandPanel.Controls.Add(stationsLabel, 0, 4)
        brandPanel.Controls.Add(accentBar, 0, 5)
        brandPanel.Controls.Add(brandFooter, 0, 6)
        loginTable.Controls.Add(welcomeLabel, 0, 0)
        loginTable.Controls.Add(subtitleLabel, 0, 1)
        loginTable.Controls.Add(usernameLabel, 0, 2)
        loginTable.Controls.Add(usernameBox, 0, 3)
        loginTable.Controls.Add(passwordLabel, 0, 4)
        loginTable.Controls.Add(passwordBox, 0, 5)
        loginTable.Controls.Add(signInButton, 0, 6)
        loginTable.Controls.Add(registerButton, 0, 7)
        loginTable.Controls.Add(demoLabel, 0, 8)
        loginTable.Controls.Add(presentationCheck, 0, 9)
        AcceptButton = signInButton
        AutoScaleDimensions = New SizeF(96.0F, 96.0F)
        AutoScaleMode = AutoScaleMode.Dpi
        BackColor = Color.FromArgb(241, 246, 251)
        ClientSize = New Size(1120, 760)
        Controls.Add(shell)
        Font = New Font("Segoe UI", 14.0F)
        ForeColor = Color.FromArgb(16, 44, 70)
        MinimumSize = New Size(900, 680)
        Name = "Form1"
        StartPosition = FormStartPosition.CenterScreen
        Text = "Welcome aboard | Group1 Transport"
        ResumeLayout(False)
    End Sub

    Friend WithEvents shell As TableLayoutPanel
    Friend WithEvents brandPanel As TableLayoutPanel
    Friend WithEvents logoPicture As PictureBox
    Friend WithEvents companyLabel As Label
    Friend WithEvents taglineLabel As Label
    Friend WithEvents journeyLabel As Label
    Friend WithEvents stationsLabel As Label
    Friend WithEvents accentBar As Panel
    Friend WithEvents brandFooter As Label
    Friend WithEvents loginHost As Panel
    Friend WithEvents loginCard As Panel
    Friend WithEvents loginTable As TableLayoutPanel
    Friend WithEvents welcomeLabel As Label
    Friend WithEvents subtitleLabel As Label
    Friend WithEvents usernameLabel As Label
    Friend WithEvents usernameBox As TextBox
    Friend WithEvents passwordLabel As Label
    Friend WithEvents passwordBox As TextBox
    Friend WithEvents signInButton As Button
    Friend WithEvents registerButton As Button
    Friend WithEvents demoLabel As Label
    Friend WithEvents presentationCheck As CheckBox
End Class
