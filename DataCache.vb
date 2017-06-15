Module DataCache
    Private dataCache As New Hashtable()
    Private datFile As String = Environment.GetEnvironmentVariable("USERPROFILE") + "\AppData\Local\SeppClient\SeppClient.cache"

    Friend Function getDataCache(pCat As String, cKey As String) As String
        Dim retValue As String = ""
        For Each de As DictionaryEntry In dataCache
            If de.Key.ToString = cKey Then
                retValue = CType(de.Value, String)
                Exit For
            End If
        Next de
        If retValue = "" Then
            retValue = Interaction.readIniFile(pCat, cKey, datFile)
        End If
        Return retValue
    End Function

    Friend Function setDataCache(pCat As String, cKey As String, cValue As String) As Boolean
        Try
            Interaction.WriteINIFile(pCat, cKey, cValue, datFile)
            If dataCache.ContainsKey(cKey) Then
                dataCache.Remove(cKey)
            End If
            dataCache.Add(cKey, cValue)
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

End Module
