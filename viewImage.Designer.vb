<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class viewImage
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(viewImage))
        Me.Panel1 = New System.Windows.Forms.Panel()
        Me.imgBox = New System.Windows.Forms.PictureBox()
        Me.Panel1.SuspendLayout()
        CType(Me.imgBox, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'Panel1
        '
        Me.Panel1.AutoScroll = True
        Me.Panel1.Controls.Add(Me.imgBox)
        Me.Panel1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.Panel1.Location = New System.Drawing.Point(0, 0)
        Me.Panel1.Name = "Panel1"
        Me.Panel1.Size = New System.Drawing.Size(1189, 770)
        Me.Panel1.TabIndex = 1
        '
        'imgBox
        '
        Me.imgBox.Location = New System.Drawing.Point(0, 0)
        Me.imgBox.Name = "imgBox"
        Me.imgBox.Size = New System.Drawing.Size(75, 102)
        Me.imgBox.TabIndex = 1
        Me.imgBox.TabStop = False
        '
        'viewImage
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 12.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.AutoScroll = True
        Me.ClientSize = New System.Drawing.Size(1189, 770)
        Me.Controls.Add(Me.Panel1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "viewImage"
        Me.Text = "SeppClient - View Image"
        Me.Panel1.ResumeLayout(False)
        CType(Me.imgBox, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents Panel1 As Panel
    Friend WithEvents imgBox As PictureBox
End Class
