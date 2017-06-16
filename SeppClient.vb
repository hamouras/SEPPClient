Imports System.IO

Public Class SeppClient
    Private AppDataDir As String = Environment.GetEnvironmentVariable("USERPROFILE") + "\AppData\Local\SeppClient"
    Private clientVersion As String = Application.ProductVersion
    Private noLogDups As String = ""

    Private Sub tailTimer_Tick(sender As Object, e As EventArgs) Handles tailTimer.Tick
        tailTimer.Enabled = False
        Dim waitForCompletion As Boolean = Files.tailJournal()
        tailTimer.Enabled = True
    End Sub

    Private Sub SeppClient_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If Not Directory.Exists(AppDataDir) Then
            My.Computer.FileSystem.CreateDirectory(AppDataDir)
        End If
        Parameters.initDefaultParameters()  ' Call this first to set default values
        Files.initJournalCodes()
        Comms.initCommsCodes()
        Username.Text = Parameters.getParameter("Username")
        SiteKey.Text = Parameters.getParameter("SiteKey")
        JournalFolder.Text = Parameters.getParameter("JournalDirectory")
        Dim bw As String = Parameters.getParameter("BlackAndWhile")
        If bw = "True" Then
            BlackAndWhile.Checked = True
        End If
        resizeSlider.Value = CInt(Parameters.getParameter("resizeValue"))
        resizeValue.Text = "Resize: " + CType((resizeSlider.Value / 4) + 1, String) + "x"
        SaveConnDetails.Enabled = False
        SaveJournalDir.Enabled = False
        SystemsList.Items.Clear()
        ConnStatus2.Text = ""
        SystemName.Text = DataCache.getDataCache("Store", "LastSystem")
        ShipName.Text = DataCache.getDataCache("Store", "LastShip")
        CommanderName.Text = DataCache.getDataCache("Store", "LastCommander")
        SeppActivity.Items.Add("Logon + All Sepp System Activity")
        SeppActivity.Items.Add("Logon + Jumps in Sepp Systems")
        SeppActivity.Items.Add("Logon + Docks in Sepp Systems")
        SeppActivity.Items.Add("Logon location only")
        SeppActivity.Items.Add("No Update")
        Dim procActivity As String = getParameter("UpdateSiteActivity")
        If procActivity = "A" Then
            SeppActivity.SelectedIndex = 0
        ElseIf procActivity = "N" Then
            SeppActivity.SelectedIndex = 4
        ElseIf procActivity = "J" Then
            SeppActivity.SelectedIndex = 1
        ElseIf procActivity = "D" Then
            SeppActivity.SelectedIndex = 2
        Else
            SeppActivity.SelectedIndex = 3
        End If
        Version.Text = "Version: " + clientVersion
        logOutput("AppData: " + AppDataDir)
        Me.Refresh()                     ' Ensure the app is fully loaded before 
        LoadTimer.Enabled = True         ' opening comms - avoids possible exceptions.
    End Sub

    Private Async Sub LoadTimer_Tick(sender As Object, e As EventArgs) Handles LoadTimer.Tick
        LoadTimer.Enabled = False
        ConnStatus1.Text = "Connecting..."
        ConnStatus1.Refresh()
        Me.Refresh()
        Dim CanConnect As Boolean = Await Comms.TestConn()
        commsTimer.Enabled = True
    End Sub

    Private Sub Username_TextChanged(sender As Object, e As EventArgs) Handles Username.TextChanged
        SaveConnDetails.Enabled = True
    End Sub

    Private Sub SiteKey_TextChanged(sender As Object, e As EventArgs) Handles SiteKey.TextChanged
        SaveConnDetails.Enabled = True
    End Sub

    Private Sub SaveConnDetails_Click(sender As Object, e As EventArgs) Handles SaveConnDetails.Click
        Parameters.setParameter("Username", Username.Text)
        Parameters.setParameter("SiteKey", SiteKey.Text)
        SaveConnDetails.Enabled = False
        TestConnection.Enabled = True
    End Sub

    Private Async Sub TestConnection_Click(sender As Object, e As EventArgs) Handles TestConnection.Click
        Dim CanConnect As Boolean = Await TestConn()
        TestConnection.Enabled = False
    End Sub

    Private Sub BrowserForDir_Click(sender As Object, e As EventArgs) Handles BrowserForDir.Click
        If FolderBrowser.ShowDialog() = DialogResult.OK Then
            JournalFolder.Text = FolderBrowser.SelectedPath
        End If
    End Sub

    Friend Sub toggleTailLog()
        If tailTimer.Enabled = False Then
            logOutput("Startup Journal Monitor")
            If Files.idLastJournal() Then
                tailTimer.Enabled = True
                tailLogs.Text = "Stop"
            Else
                logOutput("Unable to identify Journal Logs")
            End If
        Else
            tailTimer.Enabled = False
            tailLogs.Text = "Run"
            FileStatus.Text = "Idle"
            Files.stopJournal()
            logOutput("Shutdown Journal Monitor")
        End If
        tailLogs.Enabled = True
    End Sub

    Private Sub JournalFolder_TextChanged(sender As Object, e As EventArgs) Handles JournalFolder.TextChanged
        SaveJournalDir.Enabled = True
    End Sub

    Private Sub SaveJournalDir_Click(sender As Object, e As EventArgs) Handles SaveJournalDir.Click
        Parameters.setParameter("JournalDirectory", JournalFolder.Text)
        Dim procActivity As String = "O"
        If SeppActivity.SelectedIndex = 0 Then
            procActivity = "A"
        ElseIf SeppActivity.SelectedIndex = 4 Then
            procActivity = "N"
        ElseIf SeppActivity.SelectedIndex = 1 Then
            procActivity = "J"
        ElseIf SeppActivity.SelectedIndex = 2 Then
            procActivity = "D"
        End If
        setParameter("UpdateSiteActivity", procActivity)
        SaveJournalDir.Enabled = False
    End Sub

    Private Sub tailLogs_Click_1(sender As Object, e As EventArgs) Handles tailLogs.Click
        toggleTailLog()
    End Sub

    Friend Sub logOutput(logText As String)
        Try
            If noLogDups <> logText Then
                logOut.AppendText(Now().ToString + " - " + logText + vbNewLine)
                If Tabs.SelectedTab Is LogTab Then
                    LogTab.Text = "Log"
                Else
                    LogTab.Text = "Log *"
                End If
            End If
        Catch ex As Exception
            logOut.Text = "" ' Guess it's full - emptying is a harsh workaround but lets see if it ever happens
        End Try
    End Sub

    Private Sub Tabs_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Tabs.SelectedIndexChanged
        If Tabs.SelectedTab Is ChatTab Then
            ChatTab.Text = "Chat"
        ElseIf Tabs.SelectedTab Is LogTab Then
            LogTab.Text = "Log"
        End If
    End Sub

    Friend Sub chatOutput(chatText As String)
        If Strings.Left(chatText, 4) = "From" Then
            chatOut.SelectionColor = Color.DarkBlue
        Else
            chatOut.SelectionColor = Color.DarkGray
        End If
        Try
            chatOut.AppendText(Now().ToString + " - " + chatText + vbNewLine)
            If Tabs.SelectedTab Is ChatTab Then
                ChatTab.Text = "Chat"
            Else
                ChatTab.Text = "Chat *"
            End If
        Catch ex As Exception
            chatOut.Text = "" ' Guess it's full - emptying is a harsh workaround but lets see if it ever happens
        End Try

    End Sub

    Private Sub CaptureEDScreen_Click(sender As Object, e As EventArgs) Handles CaptureEDScreen.Click
        Dim bounds As Rectangle
        Dim screenshot As System.Drawing.Bitmap
        Dim graph As Graphics
        Dim overScan As Double = CInt(Parameters.getParameter("overScan")) / 100
        bounds = Screen.PrimaryScreen.Bounds
        screenshot = New System.Drawing.Bitmap(CInt((bounds.Width * overScan) / 3), CInt(bounds.Height * overScan), System.Drawing.Imaging.PixelFormat.Format24bppRgb) ' Format32bppArgb
        graph = Graphics.FromImage(screenshot)
        graph.CopyFromScreen(CInt(bounds.X * overScan), CInt(bounds.Y * overScan), 0, 0, bounds.Size, CopyPixelOperation.SourceCopy)
        completeEDScreen(screenshot)
    End Sub

    Private Sub PasteEDScreen_Click(sender As Object, e As EventArgs) Handles PasteEDScreen.Click
        Dim screenshot As System.Drawing.Bitmap
        If My.Computer.Clipboard.ContainsImage Then
            screenshot = CType(My.Computer.Clipboard.GetImage, Bitmap)
            '        EDCapture.Image = My.Computer.Clipboard.GetImage
            completeEDScreen(screenshot)
        End If
    End Sub

    Private Sub completeEDScreen(screenshot As System.Drawing.Bitmap)
        statusLabel.Text = "Working..."
        ocrWorking.Visible = True
        ocrWorking.Refresh()
        Dim resize As Double = (resizeSlider.Value / 4) + 1
        Dim procBitmap As Bitmap
        If resize > 1 Then
            procBitmap = New Bitmap(CInt(screenshot.Width * resize), CInt(screenshot.Height * resize))
            Dim grBitmap As Graphics = Graphics.FromImage(procBitmap)
            grBitmap.DrawImage(screenshot, 0, 0, procBitmap.Width + 1, procBitmap.Height + 1)
        Else
            procBitmap = screenshot
        End If
        If BlackAndWhile.Checked Then
            Dim procImg As Bitmap = toGrayScale(procBitmap)
            Call Global.SeppClient.procEDScreen(procImg)
            EDCapture.Image = procImg
        Else
            Call Global.SeppClient.procEDScreen(procBitmap)
            EDCapture.Image = procBitmap
        End If
        EDCapture.Refresh()
        ocrWorking.Visible = False
        Call Global.SeppClient.procOCRTextChg()
    End Sub

    Private Function toGrayScale(ByVal bmp As Bitmap) As Bitmap
        Dim grayscale As New Imaging.ColorMatrix(New Single()() _
        {
              New Single() {0.299F, 0.299F, 0.299F, 0, 0},
              New Single() {0.587F, 0.587F, 0.587F, 0, 0},
              New Single() {0.114F, 0.114F, 0.114F, 0, 0},
              New Single() {0, 0, 0, 1, 0},
              New Single() {0, 0, 0, 0, 1}
        })

        Dim imgattr As New Imaging.ImageAttributes()
        imgattr.SetColorMatrix(grayscale)
        Using g As Graphics = Graphics.FromImage(bmp)
            g.DrawImage(bmp, New Rectangle(0, 0, bmp.Width, bmp.Height),
                    0, 0, bmp.Width, bmp.Height,
                    GraphicsUnit.Pixel, imgattr)
        End Using

        toGrayScale = bmp
    End Function

    Private Sub UpdSoftData_Click(sender As Object, e As EventArgs) Handles UpdSoftData.Click

        For Each row As DataGridViewRow In SoftDataGrid.Rows
            Dim waitForCompletion As Boolean = Comms.sendUpdate("", "", "", selSystem.SelectedItem.ToString + ":" + row.Cells(0).Value.ToString + ":" + row.Cells(2).Value.ToString + ":" + row.Cells(1).Value.ToString)
        Next
        logOutput("Updated " + SoftDataGrid.Rows.Count.ToString + " Factions in " + selSystem.SelectedItem.ToString)
        SoftDataGrid.Rows.Clear()
        Call Global.SeppClient.procOCRTextChg()
    End Sub

    Private Async Sub commsTimer_Tick(sender As Object, e As EventArgs) Handles commsTimer.Tick
        Try
            Await Comms.procUpdate()
        Catch ex As Exception

        End Try
    End Sub

    Private Sub selSystem_SelectedIndexChanged(sender As Object, e As EventArgs) Handles selSystem.SelectedIndexChanged
        If selSystem.SelectedItem.ToString <> "" Then
            Call Global.SeppClient.procSystemChange(selSystem.SelectedItem.ToString)
        End If
    End Sub

    Private Sub EDCapture_Click(sender As Object, e As EventArgs) Handles EDCapture.Click
        If Not EDCapture.Image Is Nothing Then
            viewImage.imgBox.Width = EDCapture.Image.Width
            viewImage.imgBox.Height = EDCapture.Image.Height
            viewImage.imgBox.Image = EDCapture.Image
            viewImage.Show()
        End If
    End Sub

    Private Sub SoftDataGrid_CellValueChanged(sender As Object, e As DataGridViewCellEventArgs) Handles SoftDataGrid.CellValueChanged
        Call Global.SeppClient.procOCRTextChg()
    End Sub

    Private Sub resizeSlider_Scroll(sender As Object, e As EventArgs) Handles resizeSlider.Scroll
        resizeValue.Text = "Resize: " + CType((resizeSlider.Value / 4) + 1, String) + "x"
        Parameters.setParameter("resizeValue", resizeSlider.Value.ToString)
    End Sub

    Private Sub BlackAndWhile_CheckedChanged(sender As Object, e As EventArgs) Handles BlackAndWhile.CheckedChanged
        If BlackAndWhile.Checked Then
            Parameters.setParameter("BlackAndWhile", "True")
        Else
            Parameters.setParameter("BlackAndWhile", "False")
        End If
    End Sub

    Private Sub onTop_CheckedChanged(sender As Object, e As EventArgs) Handles onTop.CheckedChanged
        If onTop.Checked Then
            Me.TopMost = True
        Else
            Me.TopMost = False
        End If
    End Sub

    Private Sub SystemName_TextChanged(sender As Object, e As EventArgs) Handles SystemName.TextChanged
        subSystemName.Text = SystemName.Text
    End Sub

    Private Sub subSystemName_TextChanged(sender As Object, e As EventArgs) Handles subSystemName.TextChanged
        If subSystemName.Text = "" Then
            subSubmit.Enabled = False
        Else
            subSubmit.Enabled = True
        End If
    End Sub

    Private Sub subSubmit_Click(sender As Object, e As EventArgs) Handles subSubmit.Click
        logOutput("Submitted " + subSystemName.Text + " as a Point of Interest")
        Dim waitForCompletion As Boolean = Comms.sendUpdate("6", "6", subSystemName.Text + ":" + subComments.Text, "")
        subComments.Text = ""
    End Sub

    Friend Function getVersion() As String
        Return clientVersion
    End Function

    Private Sub SeppActivity_SelectedIndexChanged(sender As Object, e As EventArgs) Handles SeppActivity.SelectedIndexChanged
        SaveJournalDir.Enabled = True
    End Sub

    Private Sub SoftDataGrid_CellContentClick(sender As Object, e As DataGridViewCellEventArgs) Handles SoftDataGrid.CellContentClick

    End Sub
End Class