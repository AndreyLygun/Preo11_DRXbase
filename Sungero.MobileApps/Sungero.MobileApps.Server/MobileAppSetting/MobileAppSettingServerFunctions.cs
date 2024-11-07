using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.MobileApps.MobileAppSetting;
using Sungero.MobileAppsExtensions;

namespace Sungero.MobileApps.Server
{
  partial class MobileAppSettingFunctions
  {
    /// <summary>
    /// Отправка сообщения об изменении фильтра папок.
    /// </summary>
    [Remote(IsPure = true)]
    public virtual void SendMobileAppSettingChanged()
    {
      var args = Structures.MobileAppSetting.MobileAppSettingChangedEventArgs.Create(_obj.Employee.Id);
      Sungero.MobileAppsExtensions.MessageQueueSender.Send(Constants.Module.MobileAppQueueName, args);
    }

    /// <summary>
    /// Проверяет уникальность настройки сотрудника.
    /// </summary>
    /// <returns>Признак уникальности настройки сотрудника.</returns>
    [Remote(IsPure = true)]
    public virtual bool IsUnique()
    {
      return !MobileAppSettings.GetAll()
        .Any(t => t.Id != _obj.Id && t.Employee.Id == _obj.Employee.Id);
    }

    /// <summary>
    /// Взвращает названия папок сотрудника с их ИД.
    /// </summary>
    /// <returns>Список названий папок сотрудника с их ИД.</returns>
    [Remote(IsPure = true)]
    public virtual List<Structures.MobileAppSetting.FolderInfo> GetFolderInfos()
    {
      if (_obj.Employee == null)
        return new List<Structures.MobileAppSetting.FolderInfo>();

      var folders = this.GetFolders();
      var specialFolderMetas = this.GetSpecialFolderMetas();
      var specialFolders = this.GetSpecialFolders(specialFolderMetas);

      this.UnionFolders(folders, specialFolders);

      return this.GetFolderNameWithIds(folders, specialFolderMetas);
    }

    /// <summary>
    /// Возвращает список подготовленных названий папок сотрудника с их ИД.
    /// </summary>
    /// <param name="folderGroups">Словарь сгрупированных по корневому элементу папок.</param>
    /// <param name="specialFolderMetas">Словарь с метаданными спецпапок.</param>
    /// <returns>Cписок подготовленных названий папок сотрудника с их ИД.</returns>
    public virtual List<Structures.MobileAppSetting.FolderInfo> GetFolderNameWithIds(
      System.Collections.Generic.Dictionary<Guid, List<IFolderBase>> folderGroups,
      System.Collections.Generic.Dictionary<Guid, Sungero.Metadata.SpecialFolderMetadata> specialFolderMetas)
    {
      var result = new List<Structures.MobileAppSetting.FolderInfo>();
      foreach (var folderGroup in folderGroups)
      {
        foreach (var folder in folderGroup.Value)
        {
          if (_obj.VisibleFolders.Any(t => t.FolderId == folder.Id))
            continue;

          var readableName = this.GetFolderReadableName(folder, folderGroup.Key, specialFolderMetas);
          var isOnline = this.IsOnlineFolder(folder, folderGroup.Key);
          var entry = Structures.MobileAppSetting.FolderInfo.Create(folder.Id, readableName, isOnline);
          result.Add(entry);
        }
      }

      return result.OrderBy(t => t.FolderName).ToList();
    }

    /// <summary>
    /// Объединяет словари с папками.
    /// </summary>
    /// <param name="destination">Итоговый словарь.</param>
    /// <param name="source">Добавляемый словарь.</param>
    public virtual void UnionFolders(
      System.Collections.Generic.Dictionary<Guid, List<IFolderBase>> destination,
      System.Collections.Generic.Dictionary<Guid, List<IFolderBase>> source)
    {
      foreach (var sourceValue in source)
      {
        if (!destination.ContainsKey(sourceValue.Key))
          destination[sourceValue.Key] = new List<IFolderBase>();

        var foldersHashSet = new System.Collections.Generic.HashSet<IFolderBase>(destination[sourceValue.Key]);
        foldersHashSet.UnionWith(sourceValue.Value);

        destination[sourceValue.Key] = foldersHashSet.ToList();
      }
    }

    /// <summary>
    /// Получает словарь с метаданных спецпапок и папок потока.
    /// </summary>
    /// <returns>Cловарь с метаданных спецпапок и папок потока.</returns>
    public virtual System.Collections.Generic.Dictionary<Guid, Sungero.Metadata.SpecialFolderMetadata> GetSpecialFolderMetas()
    {
      var inboxFolderType = Sungero.Workflow.SpecialFolders.Inbox.SpecialFolderType;
      var outboxFolderType = Sungero.Workflow.SpecialFolders.Outbox.SpecialFolderType;
      var shared = Sungero.Core.SpecialFolders.Shared;
      var favorites = Sungero.Core.SpecialFolders.Favorites;
      var bookmarksGuid = Sungero.Domain.Shared.DomainFolders.MyFolder;

      return Sungero.Metadata.Services.MetadataService.Instance.AllSpecialFolders
        .Where(x => inboxFolderType == x.ParentFolderId
               || outboxFolderType == x.ParentFolderId
               || shared.SpecialFolderType == x.NameGuid
               || favorites.SpecialFolderType == x.NameGuid
               || bookmarksGuid == x.NameGuid
               || inboxFolderType == x.NameGuid
               || outboxFolderType == x.NameGuid)
        .ToDictionary(x => x.NameGuid);
    }

    /// <summary>
    /// Получает список папок сотрудника.
    /// </summary>
    /// <returns>Cписок папок сотрудника.</returns>
    public virtual System.Collections.Generic.Dictionary<Guid, List<IFolderBase>> GetFolders()
    {
      var inboxFolder = Sungero.Workflow.SpecialFolders.GetInbox(_obj.Employee);
      var outboxFolder = Sungero.Workflow.SpecialFolders.GetOutbox(_obj.Employee);
      var favorites = Sungero.Core.SpecialFolders.GetFavorites(_obj.Employee);

      var folders = new System.Collections.Generic.Dictionary<Guid, List<IFolderBase>>();
      var inboxItems = inboxFolder.Items.Where(t => (Folders.Is(t) || SearchFolders.Is(t)));
      var outboxItems = outboxFolder.Items.Where(t => (Folders.Is(t) || SearchFolders.Is(t)));

      folders[Guid.Empty] = new List<IFolderBase>();
      folders[Guid.Empty].Add(inboxFolder);
      folders[Guid.Empty].Add(outboxFolder);
      folders[Guid.Empty].Add(favorites);

      folders[inboxFolder.SpecialFolderType.Value] = new List<IFolderBase>();
      folders[inboxFolder.SpecialFolderType.Value].AddRange(inboxItems.Where(t => Folders.Is(t)).Select(t => Folders.As(t)));
      folders[inboxFolder.SpecialFolderType.Value].AddRange(inboxItems.Where(t => SearchFolders.Is(t)).Select(t => SearchFolders.As(t)));

      folders[outboxFolder.SpecialFolderType.Value] = new List<IFolderBase>();
      folders[outboxFolder.SpecialFolderType.Value].AddRange(outboxItems.Where(t => Folders.Is(t)).Select(t => Folders.As(t)));
      folders[outboxFolder.SpecialFolderType.Value].AddRange(outboxItems.Where(t => SearchFolders.Is(t)).Select(t => SearchFolders.As(t)));

      var bookmarksGuid = Sungero.Domain.Shared.DomainFolders.MyFolder;
      var bookmarksFolder = Folders.GetAll().FirstOrDefault(t => t.Author == _obj.Employee && t.SpecialFolderType == bookmarksGuid);

      if (bookmarksFolder == null)
        return folders;

      var bookmarksItems = bookmarksFolder.Items.Where(t => (Folders.Is(t) || SearchFolders.Is(t)));
      folders[bookmarksGuid] = new List<IFolderBase>();
      folders[bookmarksGuid].AddRange(bookmarksItems.Where(t => Folders.Is(t)).Select(t => Folders.As(t)).Where(t => !t.IsComputable));
      folders[bookmarksGuid].AddRange(bookmarksItems.Where(t => SearchFolders.Is(t)).Select(t => SearchFolders.As(t)));

      return folders;
    }

    /// <summary>
    /// Получает спецпапки и доступные папки потока с учетом вложенности.
    /// </summary>
    /// <param name="specialFolderMetas">Словарь с метаданными спецпапок с учетом вложенности.</param>
    /// <returns>Словарь со спецпапками и папками потока с учетом вложенности.</returns>
    public virtual System.Collections.Generic.Dictionary<Guid, List<IFolderBase>> GetSpecialFolders(System.Collections.Generic.Dictionary<Guid, Sungero.Metadata.SpecialFolderMetadata> specialFolderMetas)
    {
      var allowedSpecialFolders = this.GetEmployeeSpecialFolderGuids();

      var folders = Folders.GetAll()
        .Where(t => allowedSpecialFolders.Contains(t.SpecialFolderType.Value))
        .ToArray();

      var result = new System.Collections.Generic.Dictionary<Guid, List<IFolderBase>>();
      foreach (var folder in folders)
      {
        var meta = specialFolderMetas[folder.SpecialFolderType.Value];
        if (!result.ContainsKey(meta.ParentFolderId))
          result[meta.ParentFolderId] = new List<IFolderBase>();

        result[meta.ParentFolderId].Add(folder);
      }

      return result;
    }

    /// <summary>
    /// Получает список GUID специальных папок сотрудника.
    /// </summary>
    /// <returns>Cписок GUID специальных папок сотрудника.</returns>
    public virtual List<Guid?> GetEmployeeSpecialFolderGuids()
    {
      var allowedSpecialFolders = Sungero.MobileAppsExtensions.SpecialFolders.GetSubfolderGuids(
        _obj.Employee.Id,
        Sungero.Workflow.SpecialFolders.Inbox.SpecialFolderType.Value,
        Sungero.Workflow.SpecialFolders.Outbox.SpecialFolderType.Value)
        .ToList();

      allowedSpecialFolders.Add(Sungero.Core.SpecialFolders.Shared.SpecialFolderType.Value);
      return allowedSpecialFolders;
    }

    /// <summary>
    /// Получает список ИД специальных папок сотрудника.
    /// </summary>
    /// <returns>Cписок ИД специальных папок сотрудника.</returns>
    public virtual List<long> GetEmployeeSpecialFolderIds()
    {
      var allowedSpecialFolders = Sungero.MobileAppsExtensions.SpecialFolders.GetSubfolderIds(
        _obj.Employee.Id,
        Sungero.Workflow.SpecialFolders.Inbox.SpecialFolderType.Value,
        Sungero.Workflow.SpecialFolders.Outbox.SpecialFolderType.Value)
        .ToList();

      allowedSpecialFolders.Add(Sungero.Core.SpecialFolders.Shared.Id);
      return allowedSpecialFolders;
    }

    /// <summary>
    /// Получает человекочитаемое название папки.
    /// </summary>
    /// <param name="folderBase">Проверяемая папка.</param>
    /// <param name="parent">Родительская папка.</param>
    /// <param name="specialFolderMetas">Метаданные спецпапок.</param>
    /// <returns>Человекочитаемое название папки.</returns>
    /// <remarks>Guid преобразовывается в строку, для вложенных папок указывается корневая.</remarks>
    public virtual string GetFolderReadableName(IFolderBase folderBase, Guid parent, System.Collections.Generic.Dictionary<Guid, Sungero.Metadata.SpecialFolderMetadata> specialFolderMetas)
    {
      var parentInfo = string.Empty;
      if (parent != Guid.Empty)
      {
        var parentMeta = specialFolderMetas[parent];
        if (parent == Sungero.Domain.Shared.DomainFolders.MyFolder)
          parentInfo = string.Format("{0} / ", Sungero.MobileApps.MobileAppSettings.Resources.Bookmarks);
        else
          parentInfo = string.Format("{0} / ", Sungero.Domain.Shared.ResourceService.Instance.GetString(parentMeta.ParentModuleMetadata, parentMeta.NameResourceKey, true));
      }

      if (SearchFolders.Is(folderBase))
        return string.Format("{0}{1}", parentInfo, folderBase.Name);

      var folder = Folders.As(folderBase);
      if (!folder.IsSpecial ?? true)
        return string.Format("{0}{1}", parentInfo, folder.Name);

      var specialFolderMeta = specialFolderMetas[folder.SpecialFolderType.Value];
      return string.Format("{0}{1}", parentInfo, Sungero.Domain.Shared.ResourceService.Instance.GetString(specialFolderMeta.ParentModuleMetadata, specialFolderMeta.NameResourceKey, true));
    }

    /// <summary>
    /// Получает признак Онлайн-папки.
    /// </summary>
    /// <param name="folderBase">Проверяемая папка.</param>
    /// <param name="parent">Родительская папка.</param>
    /// <returns>Признак Онлайн-папки.</returns>
    public virtual bool IsOnlineFolder(IFolderBase folderBase, Guid parent)
    {
      if (_obj.IsAllFoldersOnline ?? false)
        return true;

      var outboxFolderType = Sungero.Workflow.SpecialFolders.Outbox.SpecialFolderType;

      if (parent == outboxFolderType)
        return true;

      var folder = Folders.As(folderBase);
      if (folder == null)
        return false;

      return (folder.IsSpecial ?? false) && folder.SpecialFolderType.Value == outboxFolderType;
    }
  }
}