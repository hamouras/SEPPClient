Imports System.Net.Sockets
Imports System.Text
Module Comms
    Private gotSystems As Boolean = False
    Private responceCodes As New Hashtable()
    Private systemFactions As New Hashtable()
    Private sendQueue As New Queue()
    Private sendCmdQueue As New Queue()
    Private sendSoftDataQueue As New Queue()
    Private lastSendTime As DateTime = DateTime.Now
    Private doRecv As Boolean = False
    Private tcpClient As New Global.System.Net.Sockets.TcpClient()
    Private authenticated As Boolean = False
    Private bytesSent As Long = 0
    Private bytesRecv As Long = 0
    Private keepAliveCount As Integer = 0

    Friend Async Function TestConn() As Task(Of Boolean)
        If Await connect() Then
            Await SendTCP("Hi:" + SeppClient.getVersion, False)
        End If
        Return False
    End Function

    Private Async Function connect() As Task(Of Boolean)
        Dim HostAddress As String = Parameters.getParameter("HostAddress")
        Dim HostPort As Integer = CInt(Parameters.getParameter("HostPort"))
        Try
            tcpClient.ReceiveTimeout = 15
            Await tcpClient.ConnectAsync(HostAddress, HostPort)
        Catch ex As Exception
            SeppClient.ConnStatus1.Text = "Cannot Connect to " + HostAddress + vbNewLine + "Check Hostname and Port" + vbNewLine + ex.Message
            SeppClient.logOutput("Connection Failed - Invalid Hostname or Port")
            SeppClient.ConnStatus1.ForeColor = Color.DarkRed
            Return False
        End Try
        Return True
    End Function

    Friend Function sendUpdate(type As String, subtype As String, data As String, softData As String) As Boolean
        Try
            If softData <> "" Then
                sendSoftDataQueue.Enqueue("7:" + softData)
            Else
                sendQueue.Enqueue(type + ":" + subtype + ":" + data + "!" + getParameter("UpdateSiteActivity"))
            End If
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Friend Async Function procUpdate() As Task
        If Not tcpClient.Connected And authenticated Then
            Try
                Dim waitForConnect As Boolean = Await connect()
            Catch ex As Exception

            End Try
        ElseIf doRecv Then
            doRecv = False
            Try
                Await recvTCP()
            Catch ex As Exception

            End Try
        Else
            doRecv = True
            If authenticated Then
                Try
                    Dim sendData As String = ""
                    Dim keepAlive As Boolean = False
                    Dim elapsedTime As TimeSpan = DateTime.Now.Subtract(lastSendTime)
                    Dim elapsedMilliSecs As Double = elapsedTime.TotalMilliseconds

                    If sendCmdQueue.Count > 0 Then
                        sendData = CType(sendCmdQueue.Peek(), String)
                        sendCmdQueue.Dequeue()
                    ElseIf sendSoftDataQueue.Count > 0 Then
                        sendData = CType(sendSoftDataQueue.Peek(), String)
                        sendSoftDataQueue.Dequeue()
                    ElseIf elapsedMilliSecs > 1499 And sendQueue.Count > 0 Then
                        sendData = CType(sendQueue.Peek(), String)
                        sendQueue.Dequeue()
                        lastSendTime = DateTime.Now
                    Else
                        keepAliveCount = keepAliveCount + 1
                        If keepAliveCount > 99 Then
                            sendData = "K"
                            keepAlive = True
                        End If
                    End If
                    If sendData <> "" Then
                        Await SendTCP(sendData, keepAlive)
                    End If
                Catch ex As Exception

                End Try
            End If
        End If
    End Function

    Private Sub getSystems()
        sendCmdQueue.Enqueue("1")
    End Sub

    Private Async Function SendTCP(sendData As String, keepAlive As Boolean) As Task
        If tcpClient.Connected Then
            Try
                Dim sendText As String
                If keepAlive Then
                    sendText = sendData
                Else
                    sendText = Parameters.getParameter("Username") + ":" + Parameters.getParameter("SiteKey") + "!" + sendData
                End If
                Dim networkStream As NetworkStream = tcpClient.GetStream()
                If networkStream.CanWrite And networkStream.CanRead Then
                    bytesSent = bytesSent + Len(sendText)
                    Dim sendBytes As [Byte]() = Encoding.ASCII.GetBytes(sendText & vbNewLine)
                    Await networkStream.WriteAsync(sendBytes, 0, sendBytes.Length)
                Else
                    streamError(networkStream)
                End If
            Catch ex As Exception

            End Try
            keepAliveCount = 0
        End If
    End Function

    Private Async Function recvTCP() As Task
        If tcpClient.Connected And tcpClient.ReceiveBufferSize > 0 Then
            Dim networkStream As NetworkStream = tcpClient.GetStream()
            If networkStream.CanWrite And networkStream.CanRead Then
                Dim returndata As String = ""
                Try
                    Dim bytes(tcpClient.ReceiveBufferSize) As Byte
                    Await networkStream.ReadAsync(bytes, 0, CInt(tcpClient.ReceiveBufferSize))
                    returndata = Trim(SoftData.whitelistChars(Encoding.ASCII.GetString(bytes)))
                    bytesRecv = bytesRecv + Len(returndata)
                    procReturnData(returndata)
                Catch ex As Exception
                End Try
            Else
                streamError(networkStream)
            End If
        End If
    End Function

    Private Sub procReturnData(rData As String)
        Dim elements() As String
        Dim stringSeparators() As String = {vbCrLf}
        elements = rData.Split(stringSeparators, StringSplitOptions.None)
        For Each line As String In elements
            If Left(line, 1) = "9" Then
                SeppClient.ConnStatus1.Text = "Unable to Authenticate"
                SeppClient.ConnStatus2.Text = "Check Username and Site Key"
                SeppClient.logOutput("Connection Failed - Invalid Username or Site Key")
                SeppClient.ConnStatus1.ForeColor = Color.DarkRed
                SeppClient.ConnStatus2.ForeColor = Color.DarkRed
            ElseIf Left(line, 1) = "1" Then
                authenticated = True
                SeppClient.logOutput("Connected to " + getParameter("HostAddress"))
                SeppClient.ConnStatus1.Text = "Connected"
                SeppClient.ConnStatus1.ForeColor = Color.DarkGreen
                SeppClient.ConnStatus2.ForeColor = Color.DarkGreen
                SeppClient.toggleTailLog()
                getSystems()
            ElseIf Left(line, 1) = "4" Then
                Files.setSeppSystems(line)
            ElseIf Left(line, 1) = "7" Then
                setSystemFaction(Trim(line))
            ElseIf Len(line) > 0 And Left(line, 1) <> "K" Then
                Dim otherResponce As String = Trim(line)
                If Len(otherResponce) > 0 Then
                    SeppClient.logOutput("    Server responded: " + getResponceDesc(otherResponce))
                End If
            End If
        Next
        If authenticated Then
            updBytesTx()
        End If
    End Sub

    Private Sub updBytesTx()
        Dim hSent As String
        Dim hRecv As String
        If bytesSent > 1500 Then
            hSent = CType(Math.Round(bytesSent / 1024), String) + "K"
        Else
            hSent = CType(bytesSent, String) + "B"
        End If
        If bytesRecv > 1500 Then
            hRecv = CType(Math.Round(bytesRecv / 1024), String) + "K"
        Else
            hRecv = CType(bytesRecv, String) + "B"
        End If
        SeppClient.ConnStatus2.Text = "Sent: " + hSent + "  Recv: " + hRecv
    End Sub

    Private Sub streamError(networkStream As NetworkStream)
        SeppClient.logOutput("Connection Failed - Issue with stream")
        If Not networkStream.CanRead Then
            SeppClient.ConnStatus1.Text = "cannot not write data to this stream"
            SeppClient.ConnStatus1.ForeColor = Color.DarkRed
            tcpClient.Close()
        Else
            If Not networkStream.CanWrite Then
                SeppClient.ConnStatus1.Text = "cannot read data from this stream"
                SeppClient.ConnStatus1.ForeColor = Color.DarkRed
                tcpClient.Close()
            End If
        End If
    End Sub

    Private Function getResponceDesc(rCode As String) As String
        Dim retValue As String = "Unknown Responce"
        For Each de As DictionaryEntry In responceCodes
            If CType(de.Key, String) = Left(rCode, 1) Then
                retValue = CType(de.Value, String)
                Exit For
            End If
        Next de
        Return retValue
    End Function

    Private Sub setSystemFaction(line As String)
        Dim elements() As String
        Dim stringSeparators() As String = {":"}
        elements = line.Split(stringSeparators, StringSplitOptions.None)
        Dim systemName As String = Trim(UCase(elements(2)))
        Dim factionData As String = elements(1).ToString
        For i = 3 To elements.GetUpperBound(0)
            factionData = factionData + ":" + Trim(UCase(elements(i)))
        Next

        If systemFactions.ContainsKey(systemName) Then
            systemFactions.Remove(systemName)
        End If
        systemFactions.Add(systemName, factionData)
        SeppClient.logOutput("Downloaded " + elements(1).ToString + " " + systemName + " Factions")

        SoftData.setFactions(systemName, factionData)
    End Sub

    Friend Sub getSystemFactions(systemName As String)
        Dim queueFaction As Boolean = True
        For Each de As DictionaryEntry In systemFactions
            If CType(de.Key, String) = UCase(systemName) Then
                queueFaction = False
                SoftData.setFactions(systemName, de.Value.ToString)
                Exit For
            End If
        Next de
        If queueFaction Then
            sendCmdQueue.Enqueue("8:" + systemName)
        End If

    End Sub

    Friend Sub initCommsCodes()
        responceCodes.Add("9", "Unable to Authenticate")
        responceCodes.Add("1", "Authenticated")
        responceCodes.Add("2", "Update not required")
        responceCodes.Add("3", "System Updated")
        responceCodes.Add("4", "Sepp Systems")
        responceCodes.Add("5", "Station Updated")
        responceCodes.Add("6", "Activity Recorded")
        responceCodes.Add("7", "Sepp System Faction")
        responceCodes.Add("8", "Soft Data Updated")
        responceCodes.Add("-", "Unknown command")
    End Sub
End Module