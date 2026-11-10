Public Class Form1
    Private arranging As Boolean
    Private ReadOnly runtimeFonts As New Dictionary(Of Control, Font)

    Public Sub New()
        InitializeComponent()
        AddHandler signInButton.Click, Sub() Ui.Guard(AddressOf SignIn)
        AddHandler registerButton.Click, Sub() Ui.Guard(AddressOf RegisterPassenger)
        AddHandler presentationCheck.CheckedChanged, Sub()
                                                        Ui.PresentationMode = presentationCheck.Checked
                                                        ArrangeLogin()
                                                    End Sub
        AddHandler Resize, Sub() ArrangeLogin()
        AddHandler Shown, Sub() ArrangeLogin()
        AddHandler Activated, Sub()
                                  presentationCheck.Checked = Ui.PresentationMode
                                  ArrangeLogin()
                              End Sub
        AddHandler FormClosed, Sub()
                                   For Each assigned In runtimeFonts.Values
                                       assigned.Dispose()
                                   Next
                                   runtimeFonts.Clear()
                               End Sub
    End Sub

    Private Sub ArrangeLogin()
        If arranging OrElse shell Is Nothing OrElse ClientSize.Width < 100 Then Return
        arranging = True
        Try
            Dim factor As Single = CSng(Math.Clamp(ClientSize.Width / 1120.0, 1.0, 1.3)) * If(Ui.PresentationMode, 1.12F, 1.0F)
            Dim width = Math.Min(ClientSize.Width - 40, 1560)
            Dim height = Math.Max(100, ClientSize.Height - 40)
            shell.SetBounds((ClientSize.Width - width) \ 2, 20, width, height)
            shell.ColumnStyles(0).Width = If(ClientSize.Width < 1050, 36, 45)
            shell.ColumnStyles(1).Width = If(ClientSize.Width < 1050, 64, 55)
            Dim cardWidth = Math.Min(CInt(530 * factor), Math.Max(330, loginHost.ClientSize.Width - 28))
            loginCard.Width = cardWidth
            loginCard.Padding = New Padding(CInt(24 * factor))
            SetLoginFont(welcomeLabel, 28 * factor, True)
            SetLoginFont(subtitleLabel, 14 * factor)
            SetLoginFont(usernameLabel, 14 * factor, True)
            SetLoginFont(passwordLabel, 14 * factor, True)
            SetLoginFont(usernameBox, 18 * factor)
            SetLoginFont(passwordBox, 18 * factor)
            SetLoginFont(signInButton, 16 * factor, True)
            SetLoginFont(registerButton, 14 * factor, True)
            SetLoginFont(demoLabel, 12 * factor)
            SetLoginFont(presentationCheck, 12 * factor, True)
            signInButton.Height = CInt(52 * factor)
            registerButton.Height = CInt(48 * factor)
            For Each label In {welcomeLabel, subtitleLabel, demoLabel}
                label.MaximumSize = New Size(Math.Max(100, cardWidth - loginCard.Padding.Horizontal), 0)
            Next
            loginTable.PerformLayout()
            loginCard.Height = loginTable.PreferredSize.Height + loginCard.Padding.Vertical
            loginCard.Location = New Point(Math.Max(14, (loginHost.ClientSize.Width - cardWidth) \ 2), Math.Max(14, (loginHost.ClientSize.Height - loginCard.Height) \ 2))
            Dim compact = ClientSize.Width < 1050
            SetLoginFont(companyLabel, If(compact, 23, 28) * factor, True)
            SetLoginFont(taglineLabel, If(compact, 17, 20) * factor, True)
            SetLoginFont(journeyLabel, 14 * factor)
            SetLoginFont(stationsLabel, 12 * factor, True)
            logoPicture.Size = New Size(CInt(112 * factor), CInt(112 * factor))
            brandPanel.Padding = New Padding(If(compact, 20, 30))
        Finally
            arranging = False
        End Try
    End Sub

    Private Sub SetLoginFont(control As Control, size As Single, Optional bold As Boolean = False)
        If Math.Abs(control.Font.Size - size) < 0.1 Then Return
        Dim previous As Font = Nothing
        runtimeFonts.TryGetValue(control, previous)
        Dim assigned As New Font("Segoe UI", size, If(bold, FontStyle.Bold, FontStyle.Regular))
        control.Font = assigned
        runtimeFonts(control) = assigned
        previous?.Dispose()
    End Sub

    Private Sub SignIn()
        Dim session = DataStore.Login(usernameBox.Text, passwordBox.Text)
        passwordBox.Clear()
        Hide()
        Try
            Using main As New WorkspaceForm(session)
                main.ShowDialog()
            End Using
        Finally
            DataStore.Logout(session)
            Show()
            Activate()
        End Try
    End Sub

    Private Sub RegisterPassenger()
        Using editor As New EntryDialog("Create passenger account", "Register")
            editor.AddText("username", "Username")
            editor.AddText("name", "Full name")
            editor.AddText("phone", "Ghana phone")
            editor.AddText("password", "Password", "", True)
            editor.AddText("confirm", "Confirm password", "", True)
            editor.AddNote("Use at least 8 characters. Your account starts as Passenger.")
            editor.SubmitAction = Sub()
                                      If editor.TextValue("password") <> editor.TextValue("confirm") Then Throw New ArgumentException("Passwords do not match.")
                                      DataStore.Register(editor.TextValue("username"), editor.TextValue("password"), editor.TextValue("name"), editor.TextValue("phone"))
                                      usernameBox.Text = editor.TextValue("username")
                                      MessageBox.Show("Your passenger account is ready. Sign in to book a trip.", "Account created")
                                  End Sub
            editor.ShowDialog(Me)
        End Using
    End Sub
End Class

