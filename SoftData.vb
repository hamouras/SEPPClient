Imports Emgu.CV
Imports Emgu.CV.CvEnum
Imports Emgu.CV.OCR

Module SoftData
    Private SeppOCR As Tesseract
    Private procOCRTextChange As Boolean = False
    Private selectedSystem As String = ""
    Private systemFactions(20) As String
    Private allStates As New Hashtable()
    Private numFactions As Integer = 0
    Private exeDir As String = AppDomain.CurrentDomain.BaseDirectory

    Friend Sub procEDScreen(bitmapImage As System.Drawing.Bitmap)
        Try
            procOCRTextChange = False
            SeppOCR = New Tesseract() ' OcrEngineMode.TesseractCubeCombined
            SeppOCR.SetVariable("tessedit_char_whitelist", "QWERTYUIOPASDFGHJKLZXCVBNM.0987654321%:")
            SeppOCR.Init(exeDir + "\tessdata", "eng", OcrEngineMode.TesseractOnly)
            SeppOCR.Recognize(New Image(Of [Structure].Gray, Byte)(bitmapImage))
            Dim elements() As String
            Dim stringSeparators() As String = {vbCrLf}
            elements = SeppOCR.GetText.Split(stringSeparators, StringSplitOptions.None)
            Dim catchFaction As String = ""
            Dim factionName As String = ""
            Dim influence As String = ""
            Dim influenceVal As Double = 0
            For Each line As String In elements
                ' line = whitelistChars(line)
                If catchFaction <> "" Then
                    If InStr(line, "RNMENT") = 0 Then
                        catchFaction = catchFaction + " " + Trim(line)
                    End If
                    factionName = matchFaction(catchFaction)
                    ' MsgBox(catchFaction + vbNewLine + factionName)
                    catchFaction = ""
                    influence = ""
                    influenceVal = 0

                End If
                If (Strings.Left(line, 2) = "FA" Or Strings.Mid(line, 3, 2) = "CT") And Len(line) > 10 Then
                    catchFaction = Mid(line, 10, Len(line) - 9)
                End If
                If (Strings.Left(line, 2) = "IN" Or Strings.Mid(line, 3, 2) = "FL") And factionName <> "" Then
                    influenceVal = matchInfluence(Trim(line))
                    influence = Replace(influenceVal.ToString, ",", ".")
                End If
                If (Strings.Left(line, 2) = "ST" Or Strings.Mid(line, 3, 2) = "AT") And influence <> "" And factionName <> "" Then
                    Dim s As String = matchState(Trim(line))
                    addEDCaptureText(factionName, influence, s, influenceVal)
                    factionName = ""
                    influence = ""
                    influenceVal = 0
                End If
            Next
        Catch ex As Exception
            MsgBox("Something went wrong with OCR - Try Again" + vbNewLine + ex.Message)
        End Try
        procOCRTextChange = True
    End Sub

    Private Sub addEDCaptureText(factionName As String, influence As String, state As String, influenceVal As Double)
        Dim doUpdate As Boolean = True
        For Each row As DataGridViewRow In SeppClient.SoftDataGrid.Rows
            If row.Cells(0).Value.ToString = factionName Then
                If CBool(SeppClient.SoftDataGrid.Rows(row.Index).Cells(3).Value) Then
                    doUpdate = False
                End If
                Exit For
            End If
        Next
        If doUpdate Then
            Dim markFound As Boolean = False
            If influenceVal > 0 Then
                markFound = True
            End If
            updDataGridRow(factionName, influence, state, markFound)
        End If
    End Sub

    Friend Sub updDataGridRow(factionName As String, influence As String, state As String, found As Boolean)
        Dim doInsert As Boolean = True
        For Each row As DataGridViewRow In SeppClient.SoftDataGrid.Rows
            If row.Cells(0).Value.ToString = factionName Then
                SeppClient.SoftDataGrid.Rows(row.Index).Cells(1).Value = influence
                SeppClient.SoftDataGrid.Rows(row.Index).Cells(2).Value = state
                SeppClient.SoftDataGrid.Rows(row.Index).Cells(3).Value = found
                doInsert = False
                Exit For
            End If
        Next
        If doInsert Then
            SeppClient.SoftDataGrid.Rows.Add(factionName, influence, state, found)
        End If
    End Sub

    Friend Sub procOCRTextChg()
        If procOCRTextChange Then
            Dim i As Double = 0
            For Each row As DataGridViewRow In SeppClient.SoftDataGrid.Rows
                Dim n As Double
                Try
                    n = Val(row.Cells(1).Value.ToString)
                Catch ex As Exception
                    n = -1
                End Try
                i = i + n
            Next

            SeppClient.infTotalVal.Text = i.ToString
            If i > 99.8 And i <= 100.1 Then
                SeppClient.CaptureEDScreen.Enabled = False
                SeppClient.PasteEDScreen.Enabled = False
                SeppClient.UpdSoftData.Enabled = True
                SeppClient.infTotal.ForeColor = Color.DarkGreen
                SeppClient.infTotalVal.ForeColor = Color.DarkGreen
            Else
                SeppClient.CaptureEDScreen.Enabled = True
                SeppClient.PasteEDScreen.Enabled = True
                SeppClient.UpdSoftData.Enabled = False
                SeppClient.infTotal.ForeColor = Color.DarkRed
                SeppClient.infTotalVal.ForeColor = Color.DarkRed
            End If
        End If
    End Sub

    Friend Sub procSystemChange(systemName As String)
        If systemName <> selectedSystem Then
            SeppClient.CaptureEDScreen.Enabled = True
            SeppClient.PasteEDScreen.Enabled = True
            SeppClient.UpdSoftData.Enabled = False
            SeppClient.SoftDataGrid.Rows.Clear()
            procOCRTextChg()
            Comms.getSystemFactions(systemName)
            selectedSystem = systemName
        End If
    End Sub

    Private Function matchFaction(factionName As String) As String
        Dim retFaction As String = ""
        Dim findFaction As String = Trim(UCase(factionName))
        Dim factionMatched As Boolean = False
        findFaction = Replace(findFaction, ":", "")
        findFaction = Replace(findFaction, "'", "")
        findFaction = Replace(findFaction, "  ", " ")
        findFaction = Trim(Replace(findFaction, ".", ""))
        For i = 0 To numFactions
            If systemFactions(i) = findFaction Then
                retFaction = systemFactions(i)
                factionMatched = True
                Exit For
            End If
        Next
        If Not factionMatched Then
            Dim l As Integer = Len(findFaction)
            Dim x As Integer = CInt(Math.Round(l * 0.95))
            Dim y As Integer = CInt(Math.Round(l * 0.85))
            Dim z As Integer = CInt(Math.Round(l * 0.7))
            Dim e As Integer = CInt(Math.Round(l * 0.3))
            Dim s(numFactions) As Integer
            For i = 0 To numFactions - 1
                s(i) = 0
                If InStr(systemFactions(i), Left(findFaction, x)) > 0 Then
                    s(i) = s(i) + 9
                End If
                If InStr(systemFactions(i), Right(findFaction, x)) > 0 Then
                    s(i) = s(i) + 9
                End If
                If InStr(systemFactions(i), Left(findFaction, y)) > 0 Then
                    s(i) = s(i) + 7
                End If
                If InStr(systemFactions(i), Right(findFaction, y)) > 0 Then
                    s(i) = s(i) + 7
                End If
                If InStr(systemFactions(i), Left(findFaction, z)) > 0 Then
                    s(i) = s(i) + 5
                End If
                If InStr(systemFactions(i), Right(findFaction, z)) > 0 Then
                    s(i) = s(i) + 5
                End If
                If Left(systemFactions(i), e) = Left(findFaction, e) Then
                    s(i) = s(i) + 3
                End If
                If Right(systemFactions(i), e) = Right(findFaction, e) Then
                    s(i) = s(i) + 3
                End If
            Next
            Dim maxI As Integer = -1
            Dim maxV As Integer = 0
            For i = 0 To numFactions - 1
                If s(i) > maxV Then
                    maxV = s(i)
                    maxI = i
                End If
            Next
            If maxI > -1 Then
                retFaction = systemFactions(maxI)
            End If
        End If
        Return retFaction
    End Function

    Private Function matchInfluence(edInfluence As String) As Double
        Dim s As String = Strings.Left(edInfluence, Len(edInfluence) - 1)
        s = Trim(Mid(s, 12))
        Dim n As Double
        Try
            n = Val(s)
        Catch ex As Exception
            n = 0
        End Try
        Return n
    End Function

    Private Function matchState(edState As String) As String
        Dim s As String = Replace(edState, "_", "")
        s = Replace(s, "'", "")
        s = Replace(s, ".", "")
        s = Replace(s, "-", "",)
        s = Replace(s, "D", "O", 2, 1)
        s = Replace(s, "D", "O", 3, 1)
        s = Replace(s, "ROOM", "BOOM")
        s = Replace(s, "EOOM", "BOOM")
        s = Replace(s, "EUST", "BUST")
        s = Replace(s, "RUST", "BUST")
        s = Replace(s, "LDCK", "LOCK")
        s = Replace(s, "DDWN", "DOWN")
        s = Replace(s, "IDN", "ION")

        If InStr(s, "BOOM") > 0 Then
            Return "Boom"
        End If
        If InStr(s, "BUST") > 0 Then
            Return "Bust"
        End If
        If InStr(s, "UNREST") > 0 Then
            Return "Civil unrest"
        End If
        If InStr(s, "CIVIL WAR") > 0 Then
            Return "Civil war"
        End If
        If InStr(s, "ELEC") > 0 Then
            Return "Election"
        End If
        If InStr(s, "EXPA") > 0 Then
            Return "Expansion"
        End If
        If InStr(s, "FAMI") > 0 Then
            Return "Famine"
        End If
        If InStr(s, "INVE") > 0 Then
            Return "Investment"
        End If
        If InStr(s, "LOCK") > 0 Then
            Return "Lockdown"
        End If
        If InStr(s, "OUTB") > 0 Then
            Return "Outbreak"
        End If
        If InStr(s, "RETR") > 0 Then
            Return "Retreat"
        End If
        If InStr(s, "WAR") > 0 Then
            Return "War"
        End If
        Return " "
    End Function

    Friend Function setFactions(systemName As String, factionData As String) As Boolean
        Try
            Dim elements() As String
            Dim stringSeparators() As String = {":"}

            If UCase(systemName) = UCase(SeppClient.selSystem.SelectedItem.ToString) Then
                elements = factionData.Split(stringSeparators, StringSplitOptions.None)
                For i = 0 To numFactions
                    systemFactions(i) = ""
                Next
                numFactions = 0
                For i = 1 To elements.GetUpperBound(0)
                    systemFactions(numFactions) = whitelistChars(Trim(UCase(elements(i))))
                    numFactions = numFactions + 1
                Next
                For i = 0 To numFactions - 1
                    updDataGridRow(systemFactions(i), "", "", False)
                Next
            End If
        Catch ex As Exception
            Return False
        End Try
        Return True
    End Function

    Friend Function whitelistChars(cleanString As String) As String
        Try
            Dim ch As Char, ln As Integer, x As Integer = 0
            ln = cleanString.Length
            Do While x < ln
                ch = cleanString.Chars(x)
                If Not (Char.IsLetterOrDigit(ch)) And Not (ch = " ") And Not (ch = "-") And Not (ch = ":") Then
                    cleanString = cleanString.Replace(ch, "")
                    ln -= 1
                    x -= 1
                End If
                x += 1
            Loop
        Catch ex As Exception

        End Try
        Return cleanString
    End Function

    Friend Function getNumFactions() As Integer
        Return numFactions
    End Function
End Module