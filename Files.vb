Imports System.IO
Imports System.Globalization

Module Files
    Private currentJournal As String = "NotInitalized"
    Private SeppSystems(32) As String
    Private lastMaxOffset As Long = 0
    Private line As String = ""
    Private idleCounter As Integer = 0
    Private processDocked As Boolean = True
    Private allegianceCodes As New Hashtable()
    Private economyCodes As New Hashtable()
    Private governmentCodes As New Hashtable()
    Private securityCodes As New Hashtable()
    Private stateCodes As New Hashtable()
    Private coords(3) As Integer
    Private waitForCords As String = ""

    Friend Function idLastJournal() As Boolean
        Dim JournalDir As String = getParameter("JournalDirectory")

        If Not Directory.Exists(JournalDir) Then
            SeppClient.FileStatus.ForeColor = Color.DarkRed
            SeppClient.FileStatus.Text = "ERROR - Directory not found"
            Return False
        Else
            SeppClient.FileStatus.ForeColor = Color.DarkCyan
            SeppClient.FileStatus.Text = "Folder Found"
        End If

        Try
            Dim tmpJournal As String = Directory.GetFiles(JournalDir, "Journal*.log").OrderByDescending(Function(f) New FileInfo(f).LastWriteTime).First()
            Dim fileName As String = Right(tmpJournal, (Len(tmpJournal) - InStrRev(tmpJournal, "\")))
            If tmpJournal <> currentJournal Then
                currentJournal = tmpJournal
                lastMaxOffset = 0
                SeppClient.logOutput("Tailing: " + fileName)
            End If
            SeppClient.FileStatus.ForeColor = Color.DarkGreen
            SeppClient.FileStatus.Text = "Tailing: " + fileName
        Catch ex As Exception
            SeppClient.FileStatus.ForeColor = Color.DarkRed
            SeppClient.FileStatus.Text = "ERROR - Journals Not Found"
            Return False
        End Try
        Return True
    End Function

    Friend Sub stopJournal()
        currentJournal = "NotInitalized"
        idleCounter = 0
    End Sub

    Friend Function tailJournal() As Boolean
        Dim waitForCompletion As Boolean
        If Not File.Exists(currentJournal) Then
            SeppClient.FileStatus.ForeColor = Color.DarkRed
            SeppClient.FileStatus.Text = "ERROR - File not found"
            Return False
        End If

        Try
            Dim JournalFileStream = New FileStream(currentJournal, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
            Using reader As New StreamReader(JournalFileStream)
                If lastMaxOffset = 0 Then
                    waitForCompletion = tailJournalReadLines(reader, 1)
                ElseIf reader.BaseStream.Length <> lastMaxOffset Then
                    waitForCompletion = tailJournalReadLines(reader, lastMaxOffset)
                    idleCounter = 0
                ElseIf idleCounter > 20 Then
                    idLastJournal()
                    idleCounter = 0
                Else
                    idleCounter = idleCounter + 1
                End If
            End Using
            Return True
        Catch ex As Exception
            SeppClient.FileStatus.ForeColor = Color.DarkRed
            SeppClient.FileStatus.Text = "ERROR - File could not be read"
            Return False
        End Try
    End Function

    Private Function tailJournalReadLines(reader As StreamReader, Offset As Long) As Boolean
        Try
            Dim waitForCompletion As Boolean
            reader.BaseStream.Seek(Offset, SeekOrigin.Begin)
            Do
                line = reader.ReadLine()
                waitForCompletion = filterJournalLine(line)
            Loop Until line Is Nothing
            lastMaxOffset = reader.BaseStream.Length
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function filterJournalLine(line As String) As Boolean
        ' Codes expected by the server
        '
        ' Codes
        ' -----
        ' # 2 : System
        ' # 3 : System Faction
        ' # 4 : System Faction State
        ' # 5 : Station
        ' # 6 : Activity
        ' # 7 : Chat
        '
        ' Sub-Codes
        ' ---------
        ' # 0 : Logon
        ' # 1 : FSDJump
        ' # 2 : LoadGame
        ' # 3 : Docked
        ' # 4 : ShipyardSwap
        ' # 5 : ShipyardNew
        ' # 6 : SendText (Also Activity Point of Interest - Called from sepp client)
        ' # 7 : ReceiveText
        ' # 8 : Promotion
        '

        Try
            Dim curLine As String = Replace(line, """", "|")
            Dim waitForCompletion As Boolean = Nothing
            Dim procActivity As String = getParameter("UpdateSiteActivity")
            curLine = Replace(curLine, "{", "")
            curLine = Trim(Replace(curLine, "}", ""))
            If InStr(curLine, "|event|:|Docked|") > 0 Then
                If processDocked Then
                    waitForCompletion = processJournalLine(curLine, "5", "3")
                Else
                    processDocked = True
                End If
            ElseIf InStr(curLine, "|event|:|Location|") > 0 Then
                waitForCompletion = processJournalLine(curLine, "2", "0")
            ElseIf InStr(curLine, "|event|:|FSDJump|,") > 0 Then
                waitForCompletion = processJournalLine(curLine, "2", "1")
            ElseIf InStr(curLine, "|event|:|LoadGame|,") > 0 Then
                waitForCompletion = processJournalLine(curLine, "6", "2")
            ElseIf InStr(curLine, "|event|:|Promotion|,") > 0 And procActivity <> "N" Then
                waitForCompletion = processJournalLine(curLine, "6", "8")
            ElseIf InStr(curLine, "|event|:|SendText|,") > 0 Then
                waitForCompletion = processJournalLine(curLine, "7", "6")
            ElseIf InStr(curLine, "|event|:|ReceiveText|,") > 0 Then
                waitForCompletion = processJournalLine(curLine, "7", "7")
            ElseIf InStr(curLine, "|event|:|ShipyardSwap|,") > 0 And (procActivity = "A" Or procActivity = "D") Then
                processDocked = False
                waitForCompletion = processJournalLine(curLine, "6", "4")
            ElseIf InStr(curLine, "|event|:|ShipyardNew|,") > 0 And (procActivity = "A" Or procActivity = "D") Then
                waitForCompletion = processJournalLine(curLine, "6", "5")
            End If
            Return True
        Catch ex As Exception

        End Try
        Return False
    End Function

    Private Function processJournalLine(line As String, uType As String, uSubType As String) As Boolean
        Try
            Dim waitForCompletion As Boolean
            Dim elapsedMinutes As Double = 100
            Dim elements() As String
            Dim stringSeparators() As String = {", "}
            Dim sTimeStamp As String = ""
            elements = line.Split(stringSeparators, StringSplitOptions.None)
            For Each s As String In elements
                If InStr(s, "|timestamp|:") > 0 Then  ' Timestamp is the first element, so lets get this and exit the for loop, then only proceed if its recent
                    sTimeStamp = Trim(Replace(Mid(s, 14), "|", ""))
                    sTimeStamp = Replace(sTimeStamp, "T", " ")
                    sTimeStamp = Replace(sTimeStamp, "Z", "")
                    Dim datePattern As String = "yyyy-MM-dd HH:mm:ss"
                    Dim dateParsed As Date
                    If DateTime.TryParseExact(sTimeStamp, datePattern, Nothing, DateTimeStyles.None, dateParsed) Then
                        Dim elapsedTime As TimeSpan = DateTime.UtcNow.Subtract(dateParsed)
                        elapsedMinutes = elapsedTime.TotalMinutes
                        Exit For
                    End If
                End If
            Next

            If elapsedMinutes < 2 Then
                If uType = "7" Then
                    waitForCompletion = processJournalChatLine(line, uType, uSubType)
                ElseIf uType = "6" Then
                    waitForCompletion = processJournalActivityLine(line, uType, uSubType, sTimeStamp)
                Else
                    waitForCompletion = processJournalSystemLine(line, uType, uSubType, sTimeStamp)
                End If
            End If
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function processJournalSystemLine(line As String, uType As String, uSubType As String, sTimeStamp As String) As Boolean
        Try
            Dim waitForCompletion As Boolean
            Dim elements() As String
            Dim stringSeparators() As String = {","}
            Dim systemName As String = ""
            Dim stationName As String = ""
            Dim systemAllegiance As String = ""
            Dim systemEconomy As String = ""
            Dim systemGovernment As String = ""
            Dim systemSecurity As String = ""
            Dim sFaction As String = ""
            Dim sFactionState As String = ""

            If uSubType = "0" Then
                Dim coordsStart As Integer = InStr(line, "|StarPos|:")
                If coordsStart > 0 Then
                    coordsStart = coordsStart + 11
                    Dim coordsEnd As Integer = InStr(Mid(line, coordsStart), "]")
                    Dim coordsTxt As String = Mid(line, coordsStart, coordsEnd - 1)
                    elements = coordsTxt.Split(stringSeparators, StringSplitOptions.None)
                    Dim i As Integer = 0
                    For Each coord As String In elements
                        coords(i) = CInt(coord)
                        i = i + 1
                    Next
                End If
            End If

            stringSeparators = {", "}
            elements = line.Split(stringSeparators, StringSplitOptions.None)
            For Each s As String In elements
                If InStr(s, "|StarSystem|:") > 0 Then
                    systemName = Trim(Replace(Mid(s, 15), "|", ""))
                ElseIf InStr(s, "|StationName|:") > 0 And uType = "5" Then
                    stationName = Trim(Replace(Mid(s, 16), "|", ""))
                ElseIf InStr(s, "|SystemAllegiance|:") > 0 Then
                    systemAllegiance = getAllegianceCode(Trim(Replace(Mid(s, 21), "|", "")))
                ElseIf InStr(s, "|SystemEconomy_Localised|:") > 0 Then
                    systemEconomy = getEconomyCode(Trim(Replace(Mid(s, 28), "|", "")))
                ElseIf InStr(s, "|SystemGovernment_Localised|:") > 0 Then
                    systemGovernment = getGovernmentCode(Trim(Replace(Mid(s, 31), "|", "")))
                ElseIf InStr(s, "|SystemSecurity_Localised|:") > 0 Then
                    systemSecurity = getSecurityCode(Trim(Replace(Mid(s, 29), "|", "")))
                ElseIf InStr(s, "|SystemFaction|:") > 0 Then
                    sFaction = Trim(Replace(Mid(s, 18), "|", ""))
                ElseIf InStr(s, "|StationFaction|:") > 0 Then
                    sFaction = Trim(Replace(Mid(s, 19), "|", ""))
                ElseIf InStr(s, "|FactionState|:") > 0 Then
                    sFactionState = getStateCode(Trim(Replace(Mid(s, 17), "|", "")))
                End If
            Next

            If uSubType = "0" And waitForCords <> "" Then
                Try
                    Dim dist As Double = distFromEleu(coords(0), coords(1), coords(2))
                    waitForCompletion = processActivity(waitForCords, "6", "2", sTimeStamp, ":" + dist.ToString + ":" + systemName)
                    waitForCords = ""
                Catch inEx As Exception

                End Try
            End If
            waitForCompletion = processSystemUpdate(systemName, stationName, systemAllegiance, systemEconomy, systemGovernment, systemSecurity, sFaction, sFactionState, sTimeStamp, uType, uSubType)
            If systemName <> "" Then
                SeppClient.SystemName.Text = systemName
                DataCache.setDataCache("Store", "LastSystem", systemName)
            End If
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function processJournalActivityLine(line As String, uType As String, uSubType As String, sTimeStamp As String) As Boolean
        Try
            Dim waitForCompletion As Boolean
            Dim elements() As String
            Dim stringSeparators() As String = {", "}
            Dim sShip As String = ""
            Dim promoteRank As String = ""
            Dim promoteType As String = ""

            elements = line.Split(stringSeparators, StringSplitOptions.None)
            For Each s As String In elements
                If InStr(s, "|Ship|:") > 0 Then
                    sShip = Trim(Replace(Mid(s, 9), "|", ""))
                ElseIf InStr(s, "|Commander|:") > 0 And uSubType = "2" Then
                    Dim commanderName As String = Trim(Replace(Mid(s, 14), "|", ""))
                    SeppClient.CommanderName.Text = commanderName
                    DataCache.setDataCache("Store", "LastCommander", commanderName)
                ElseIf InStr(s, "|ShipType|:") > 0 Then
                    sShip = Trim(Replace(Mid(s, 13), "|", ""))
                ElseIf InStr(s, "|Combat|:") > 0 And uSubType = "8" Then
                    promoteRank = Trim(Replace(Mid(s, 11), "|", ""))
                    promoteType = "C"
                ElseIf InStr(s, "|Trade|:") > 0 And uSubType = "8" Then
                    promoteRank = Trim(Replace(Mid(s, 10), "|", ""))
                    promoteType = "T"
                ElseIf InStr(s, "|Explore|:") > 0 And uSubType = "8" Then
                    promoteRank = Trim(Replace(Mid(s, 12), "|", ""))
                    promoteType = "E"
                ElseIf InStr(s, "|CQC|:") > 0 And uSubType = "8" Then
                    promoteRank = Trim(Replace(Mid(s, 8), "|", ""))
                    promoteType = "Q"
                End If
            Next

            If getParameter("UpdateSiteActivity") <> "N" Then
                If uSubType = "2" Then
                    waitForCords = sShip
                ElseIf uSubType = "8" Then
                    waitForCompletion = processActivity(promoteType, uType, uSubType, sTimeStamp, ":" + promoteRank)
                Else
                    waitForCompletion = processActivity(sShip, uType, uSubType, sTimeStamp, "")
                End If
            End If
            If sShip <> "" Then
                SeppClient.ShipName.Text = sShip
                DataCache.setDataCache("Store", "LastShip", sShip)
            End If
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function processJournalChatLine(line As String, uType As String, uSubType As String) As Boolean
        Try
            Dim chatType As String = ""
            Dim chatText As String = ""
            Dim chatChannel As String = ""
            Dim elements() As String
            Dim stringSeparators() As String = {", "}

            elements = line.Split(stringSeparators, StringSplitOptions.None)
            For Each s As String In elements
                If InStr(s, "|Message|:") > 0 And uType = "7" Then
                    chatText = Trim(Replace(Mid(s, 12), "|", ""))
                ElseIf InStr(s, "|To|:") > 0 And uSubType = "6" Then
                    chatType = "To: " + Trim(Replace(Mid(s, 7), "|", ""))
                ElseIf InStr(s, "|From_Localised|:") > 0 And uSubType = "7" Then
                    chatType = "From: " + Trim(Replace(Mid(s, 19), "|", ""))
                ElseIf InStr(s, "|Channel|:") > 0 And uSubType = "7" Then
                    chatChannel = Trim(Replace(Mid(s, 12), "|", ""))
                End If
            Next

            If chatChannel <> "npc" Then
                SeppClient.chatOutput(chatType + " -  " + chatText)
            End If
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function distFromEleu(x As Integer, y As Integer, z As Integer) As Integer
        Dim eleuX As Integer = -30 ' -29.656
        Dim eleuY As Integer = 33  '  32.688
        Dim eleuZ As Integer = 105 ' 104.844
        Dim dist As Integer = CInt(Math.Round(Math.Sqrt(Math.Pow((eleuX - x), 2) + Math.Pow((eleuY - y), 2) + Math.Pow((eleuZ - z), 2))))
        Return dist
    End Function

    Private Function processActivity(cKey As String, uType As String, uSubType As String, sTimeStamp As String, uExtra As String) As Boolean
        Try
            If cKey <> "" Then
                Dim DataRow As String = cKey
                Dim cCat As String = "Activity"

                If uExtra <> "" Then
                    DataRow = DataRow + uExtra
                End If
                Dim waitForCompletion As Boolean = processUpdate(DataRow, sTimeStamp, cCat, cKey, uType, uSubType)
            End If
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function processSystemUpdate(systemName As String, stationName As String, systemAllegiance As String, systemEconomy As String, systemGovernment As String, systemSecurity As String, sFaction As String, sFactionState As String, sTimeStamp As String, uType As String, uSubType As String) As Boolean
        Try
            For index = 0 To SeppSystems.GetUpperBound(0)
                If SeppSystems(index) = systemName Then
                    Dim cKey As String = systemName
                    Dim DataRow As String = ""

                    Dim cCat As String
                    If uType = "2" Then
                        cCat = "System"
                        DataRow = systemName + ":" + systemAllegiance + ":" + systemEconomy + ":" + systemGovernment + ":" + systemSecurity
                        If sFaction <> "" Then
                            If sFactionState <> "" Then
                                DataRow = DataRow + ":" + sFaction + ":" + sFactionState
                                cCat = "System Faction State"
                                uType = "4"
                            Else
                                DataRow = DataRow + ":" + sFaction
                                cCat = "System Faction"
                                uType = "3"
                            End If
                        End If
                    Else
                        cCat = "Station"
                        cKey = stationName
                        DataRow = stationName + ":" + sFaction + ":" + systemName + ":" + systemAllegiance + ":" + systemEconomy + ":" + systemGovernment ' + ":" + systemSecurity
                    End If
                    Dim waitForCompletion As Boolean = processUpdate(DataRow, sTimeStamp, cCat, cKey, uType, uSubType)
                    Exit For
                End If
            Next
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function processUpdate(DataRow As String, sTimeStamp As String, cCat As String, cKey As String, uType As String, uSubType As String) As Boolean
        Dim localCache As String = DataRow + ":" + sTimeStamp
        If DataCache.getDataCache(cCat, cKey) <> localCache Then
            If DataCache.setDataCache(cCat, cKey, localCache) Then
                Dim waitForCompletion As Boolean = Comms.sendUpdate(uType, uSubType, DataRow, "")
                SeppClient.logOutput("Sending " + cCat + " Update for " + cKey)
            End If
        Else
            SeppClient.logOutput("Skipping " + cCat + " Update for " + cKey + " - Duplicate Data")
        End If
        Return True
    End Function

    Private Function getAllegianceCode(aText As String) As String
        Dim retValue As String = "3" ' 3 = None
        For Each de As DictionaryEntry In allegianceCodes
            If LCase(de.Key.ToString) = LCase(aText) Then
                retValue = CType(de.Value, String)
                Exit For
            End If
        Next de
        Return retValue
    End Function

    Private Function getGovernmentCode(gText As String) As String
        Dim retValue As String = "13" ' 13 = None
        For Each de As DictionaryEntry In governmentCodes
            If LCase(de.Key.ToString) = LCase(gText) Then
                retValue = CType(de.Value, String)
                Exit For
            End If
        Next de
        Return retValue
    End Function

    Private Function getEconomyCode(eText As String) As String
        Dim retValue As String = "10" ' 10 = None
        For Each de As DictionaryEntry In economyCodes
            If LCase(de.Key.ToString) = LCase(eText) Then
                retValue = CType(de.Value, String)
                Exit For
            End If
        Next de
        Return retValue
    End Function

    Private Function getSecurityCode(sText As String) As String
        Dim retValue As String = "3" ' 3 = None
        For Each de As DictionaryEntry In securityCodes
            If LCase(de.Key.ToString) = LCase(sText) Then
                retValue = CType(de.Value, String)
                Exit For
            End If
        Next de
        Return retValue
    End Function

    Private Function getStateCode(sText As String) As String
        Dim retValue As String = "12" ' 12 = None
        For Each de As DictionaryEntry In stateCodes
            If LCase(de.Key.ToString) = LCase(sText) Then
                retValue = CType(de.Value, String)
                Exit For
            End If
        Next de
        Return retValue
    End Function

    Friend Sub setSeppSystems(systems As String)
        Dim elements() As String
        Dim stringSeparators() As String = {":"}
        elements = systems.Split(stringSeparators, StringSplitOptions.None)
        ReDim SeppSystems(CInt(elements(1)))
        SeppClient.SystemsList.Items.Clear()
        For index = 2 To elements.GetUpperBound(0)
            Dim cleanSystemName As String = SoftData.whitelistChars(Trim(elements(index)))
            SeppSystems(index - 1) = cleanSystemName
            SeppClient.SystemsList.Items.Add(cleanSystemName)
            SeppClient.selSystem.Items.Add(cleanSystemName)
            getSystemFactions(cleanSystemName)
        Next
        SeppClient.logOutput("Downloaded " + elements(1) + " SEPP Systems")
    End Sub

    Friend Sub initJournalCodes()
        ' Server Codes
        '
        'my @allegianceCodes = ("Independent", "Federation", "Empire", "None");
        'My @economyCodes    = ("Agriculture", "Demand", "Extraction", "High Tech", "Industrial", "Refinery", "Service", "Supply", "Terraforming", "Tourism", "None");
        'My @governmentCodes = ("Anarchy", "Colony", "Communism", "Confederacy", "Cooperative", "Corporate", "Democracy", "Dictatorship", "Feudal", "Imperial", "Patronage", "Prison Colony", "Theocracy", "None");
        'My @securityCodes   = ("High", "Low", "Medium", "None");
        'My @stateCodes      = ("Boom", "Bust", "Civil unrest", "Civil war", "Election", "Expansion", "Famine", "Investment", "Lockdown", "Outbreak", "Retreat", "War", " ");
        '#my @shipCodes      = ("Sidewinder", ...); # Waiting for the ShipID values to be documented - Use Ship or ShipType for now.

        allegianceCodes.Add("independent", "0")
        allegianceCodes.Add("federation", "1")
        allegianceCodes.Add("empire", "2")

        economyCodes.Add("agriculture", "0")
        economyCodes.Add("demand", "1")
        economyCodes.Add("extraction", "2")
        economyCodes.Add("high tech", "3")
        economyCodes.Add("industrial", "4")
        economyCodes.Add("refinery", "5")
        economyCodes.Add("service", "6")
        economyCodes.Add("supply", "7")
        economyCodes.Add("terraforming", "8")
        economyCodes.Add("tourism", "9")

        governmentCodes.Add("anarchy", "0")
        governmentCodes.Add("colony", "1")
        governmentCodes.Add("communism", "2")
        governmentCodes.Add("confederacy", "3")
        governmentCodes.Add("cooperative", "4")
        governmentCodes.Add("corporate", "5")
        governmentCodes.Add("democracy", "6")
        governmentCodes.Add("dictatorship", "7")
        governmentCodes.Add("feudal", "8")
        governmentCodes.Add("imperial", "9")
        governmentCodes.Add("patronage", "10")
        governmentCodes.Add("prison colony", "11")
        governmentCodes.Add("theocracy", "12")

        securityCodes.Add("high", "0")
        securityCodes.Add("low", "1")
        securityCodes.Add("medium", "2")

        stateCodes.Add("boom", "0")
        stateCodes.Add("bust", "1")
        stateCodes.Add("civil unrest", "2")
        stateCodes.Add("civil war", "3")
        stateCodes.Add("election", "4")
        stateCodes.Add("expansion", "5")
        stateCodes.Add("famine", "6")
        stateCodes.Add("investment", "7")
        stateCodes.Add("lockdown", "8")
        stateCodes.Add("outbreak", "9")
        stateCodes.Add("retreat", "0")
        stateCodes.Add("war", "11")

    End Sub
End Module
