using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.MobileApps.MobileAppSetting;

namespace Sungero.MobileApps.Client
{
  partial class MobileAppSettingActions
  {
    public virtual void AddFolder(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var folders = Functions.MobileAppSetting.Remote.GetFolderInfos(_obj);

      if (!folders.Any())
      {
        Dialogs.NotifyMessage(Sungero.MobileApps.MobileAppSettings.Resources.FolderListIsAlreadyUpdated);
        return;
      }

      var dialog = Dialogs.CreateInputDialog(Sungero.MobileApps.MobileAppSettings.Resources.SelectFolder);
      dialog.Text = Sungero.MobileApps.MobileAppSettings.Resources.SelectAFolderToAdd;
      var addButton = dialog.Buttons.AddCustom(Sungero.MobileApps.MobileAppSettings.Resources.AddButton);
      dialog.Buttons.AddCancel();
      var addedFolderName = dialog.AddSelect(Sungero.MobileApps.MobileAppSettings.Resources.Folder, true).From(folders.Select(t => t.FolderName).ToArray());

      if (dialog.Show() != addButton)
        return;

      var addedFolder = folders.FirstOrDefault(t => t.FolderName == addedFolderName.Value);

      var newFolder = _obj.VisibleFolders.AddNew();
      newFolder.FolderId = addedFolder.Id;
      newFolder.FolderName = addedFolder.FolderName;
      newFolder.IsOnline = addedFolder.IsOnline;
    }

    public virtual bool CanAddFolder(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Employee != null;
    }

    public virtual void Reset(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var currentOnlineFolders = _obj.VisibleFolders
        .Where(t => t.IsOnline.GetValueOrDefault())
        .Select(t => t.FolderId)
        .ToList();

      // Вместо обработки удалений и переименований.
      _obj.VisibleFolders.Clear();

      var folders = Functions.MobileAppSetting.Remote.GetFolderInfos(_obj);

      foreach (var folder in folders)
      {
        var newFolder = _obj.VisibleFolders.AddNew();
        newFolder.FolderId = folder.Id;
        newFolder.FolderName = folder.FolderName;
        newFolder.IsOnline = currentOnlineFolders.Contains(newFolder.FolderId) || folder.IsOnline;
      }

      Dialogs.NotifyMessage(Sungero.MobileApps.MobileAppSettings.Resources.FolderListIsAlreadyUpdated);
    }

    public virtual bool CanReset(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Employee != null;
    }
  }

  partial class MobileAppSettingAnyChildEntityActions
  {
    public override void AddChildEntity(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.AddChildEntity(e);
    }

    public override bool CanCopyChildEntity(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return false;
    }

    public override bool CanAddChildEntity(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return false;
    }
  }
}