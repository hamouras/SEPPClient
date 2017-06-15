Module Parameters
    Private defaultValues As New Hashtable()
    Private userValues As New Hashtable()
    Private iniFile As String = Environment.GetEnvironmentVariable("USERPROFILE") + "\AppData\Local\SeppClient\SeppClient.ini"

    Friend Sub initDefaultParameters()
        ' SeppClient Default Values
        defaultValues.Add("JournalDirectory", Environment.GetEnvironmentVariable("USERPROFILE") + "\Saved Games\Frontier Developments\Elite Dangerous\")
        defaultValues.Add("HostAddress", "sepp.space")
        defaultValues.Add("HostPort", "4526")
        defaultValues.Add("UpdateSiteActivity", "O")
        defaultValues.Add("BlackAndWhile", "False")
        defaultValues.Add("resizeValue", "4")
        defaultValues.Add("overScan", "100")
    End Sub

    Private Function getDefaultParameter(pKey As String) As String
        Dim retValue As String = ""
        For Each de As DictionaryEntry In defaultValues
            If de.Key.ToString = pKey Then
                retValue = CType(de.Value, String)
                Exit For
            End If
        Next de
        Return retValue
    End Function

    Friend Function getParameter(pKey As String) As String
        Dim retValue As String = ""
        For Each de As DictionaryEntry In userValues
            If de.Key.ToString = pKey Then
                retValue = CType(de.Value, String)
                Exit For
            End If
        Next de
        If retValue = "" Then
            retValue = Interaction.readIniFile("Client", pKey, iniFile)
            If retValue = "" Then
                retValue = getDefaultParameter(pKey)
                If retValue <> "" Then
                    setParameter(pKey, retValue)
                End If
            End If
        End If
        Return retValue
    End Function

    Friend Sub setParameter(pKey As String, pValue As String)
        Interaction.WriteINIFile("Client", pKey, pValue, iniFile)
        If userValues.ContainsKey(pKey) Then
            userValues.Remove(pKey)
        End If
        userValues.Add(pKey, pValue)
    End Sub

End Module
