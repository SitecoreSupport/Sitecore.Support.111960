// Sitecore.Shell.Applications.ContentManager.Dialogs.Aliases.AliasesForm
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Events;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.SecurityModel;
using Sitecore.Shell.Framework;
using Sitecore.Text;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Sitecore.Support.Shell.Applications.ContentManager.Dialogs.Aliases
{

  /// <summary>
  /// The aliases form.
  /// </summary>
  public class AliasesForm : DialogForm
  {
    /// <summary>
    /// Alias Info
    /// </summary>
    private class AliasInfo
    {
      /// <summary>
      /// The path.
      /// </summary>
      private readonly ListString _path;

      /// <summary>
      /// Gets the ascenders.
      /// </summary>
      /// <value>The ascenders.</value>
      public IEnumerable<string> Ascenders
      {
        get
        {
          if (_path.Count > 1)
          {
            for (int i = 0; i < _path.Count - 1; i++)
            {
              yield return _path[i];
            }
          }
        }
      }

      /// <summary>
      /// Gets the name of the ascenders and.
      /// </summary>
      /// <value>The name of the ascenders and.</value>
      public IEnumerable<string> AscendersAndName => _path.Items;

      /// <summary>
      /// Gets the name.
      /// </summary>
      /// <value>The name.</value>
      public string Name => _path[_path.Count - 1];

      /// <summary>
      /// Initializes a new instance of the <see cref="T:Sitecore.Shell.Applications.ContentManager.Dialogs.Aliases.AliasesForm.AliasInfo" /> class.
      /// </summary>
      /// <param name="value">
      /// The value.
      /// </param>
      public AliasInfo(string value)
      {
        Assert.ArgumentNotNullOrEmpty(value, "value");
        value = StringUtil.RemovePrefix("/", value);
        value = StringUtil.RemovePostfix("/", value);
        _path = new ListString(value, '/');
      }
    }

    /// <summary>
    /// The list.
    /// </summary>
    protected Listbox List;

    /// <summary></summary>
    protected Edit NewAlias;

    /// <summary></summary>
    protected Border ListHolder;

    /// <summary>
    /// Handles a click on the Add button.
    /// </summary>
    protected void Add_Click()
    {
      string value = NewAlias.Value;
      if (value.Length == 0)
      {
        SheerResponse.Alert("Enter a value in the Add Input field.");
      }
      else
      {
        AliasInfo aliasInfo = new AliasInfo(value);
        foreach (string item2 in aliasInfo.AscendersAndName)
        {
          if (!Regex.IsMatch(item2, Settings.ItemNameValidation, RegexOptions.ECMAScript))
          {
            SheerResponse.Alert("The name contains invalid characters.");
            return;
          }
          if (item2.Length > Settings.MaxItemNameLength)
          {
            SheerResponse.Alert("The name is too long.");
            return;
          }
        }
        Item itemFromQueryString = UIUtil.GetItemFromQueryString(Context.ContentDatabase);
        Error.AssertItemFound(itemFromQueryString);
        Item item = Context.ContentDatabase.GetItem("/sitecore/system/Aliases", itemFromQueryString.Language);
        Error.AssertItemFound(item, "/sitecore/system/Aliases");
        ListItem listItem = CreateAlias(aliasInfo, itemFromQueryString, item);
        if (listItem != null)
        {
          SheerResponse.Eval("scCreateAlias(" + StringUtil.EscapeJavascriptString(StringUtil.EscapeQuote(listItem.ID)) + ", " + StringUtil.EscapeJavascriptString(StringUtil.EscapeQuote(listItem.Header)) + ", " + StringUtil.EscapeJavascriptString(StringUtil.EscapeQuote(listItem.Value)) + ");");
          NewAlias.Value = string.Empty;
          SheerResponse.SetModified(false);
        }
      }
    }

    /// <summary>
    /// Raises the load event.
    /// </summary>
    /// <param name="e">
    /// The <see cref="T:System.EventArgs" /> instance containing the event data.
    /// </param>
    protected override void OnLoad(EventArgs e)
    {
      Assert.CanRunApplication("Content Editor/Ribbons/Chunks/Page Urls");
      Assert.ArgumentNotNull(e, "e");
      base.OnLoad(e);
      if (!Context.ClientPage.IsEvent)
      {
        RefreshList();
      }
      Context.Site.Notifications.ItemDeleted += ItemDeletedNotification;
    }

    /// <summary>
    /// Handles a click on the Remove button.
    /// </summary>
    protected void Remove_Click()
    {
      Item itemFromQueryString = UIUtil.GetItemFromQueryString(Context.ContentDatabase);
      Error.AssertItemFound(itemFromQueryString);
      ArrayList arrayList = new ArrayList();
      ListItem[] selected = List.Selected;
      foreach (ListItem listItem in selected)
      {
        string iD = listItem.ID;
        iD = ShortID.Decode(StringUtil.Mid(iD, 1));
        Item item = itemFromQueryString.Database.GetItem(iD);
        if (item != null)
        {
          arrayList.Add(item);
        }
      }
      if (arrayList.Count == 0)
      {
        SheerResponse.Alert("Select an alias from the list.");
      }
      else
      {
        Items.Delete(arrayList.ToArray(typeof(Item)) as Item[]);
        ListString listString = new ListString();
        foreach (Item item2 in arrayList)
        {
          listString.Add("I" + item2.ID.ToShortID());
          Log.Audit(this, "Remove alias: {0}", AuditFormatter.FormatItem(item2));
        }
        base.ServerProperties["deleted"] = listString.ToString();
        SheerResponse.SetModified(false);
      }
    }

    /// <summary>
    /// Gets the alias path.
    /// </summary>
    /// <param name="alias">
    /// The alias.
    /// </param>
    /// <returns>
    /// The alias path.
    /// </returns>
    private static string GetAliasPath(Item alias)
    {
      Assert.ArgumentNotNull(alias, "alias");
      string text = alias.Paths.GetPath("/sitecore/system/Aliases", "/", ItemPathType.Name);
      if (text.StartsWith("/", StringComparison.InvariantCulture))
      {
        text = text.Substring(1);
      }
      return text;
    }

    /// <summary>
    /// Creates the alias.
    /// </summary>
    /// <param name="aliasInfo">The alias info.</param>
    /// <param name="target">The target.</param>
    /// <param name="root">The root.</param>
    private ListItem CreateAlias(AliasInfo aliasInfo, Item target, Item root)
    {
      Assert.ArgumentNotNull(aliasInfo, "aliasInfo");
      Assert.ArgumentNotNull(target, "target");
      Assert.ArgumentNotNull(root, "root");
      TemplateItem template = root.Database.Templates["System/Alias"];
      Error.AssertTemplate(template, "Alias");
      foreach (string ascender in aliasInfo.Ascenders)
      {
        root = root.Children[ascender];
        if (root == null)
        {
          SheerResponse.Alert($"The parent alias '{ascender}' does not exist.");
          return null;
        }
      }
      if (root.Children[aliasInfo.Name] != null)
      {
        SheerResponse.Alert("An alias with this name already exists.");
        return null;
      }
      Item item = root.Add(aliasInfo.Name, template);
      item.Editing.BeginEdit();
      item["Linked Item"] = "<link linktype=\"internal\" url=\"" + target.Paths.ContentPath + "\" id=\"" + target.ID + "\" />";
      item.Editing.EndEdit();
      ListItem listItem = new ListItem();
      List.Controls.Add(listItem);
      listItem.ID = "I" + item.ID.ToShortID();
      listItem.Header = GetAliasPath(item);
      listItem.Value = item.ID.ToString();
      return listItem;
    }

    /// <summary>
    /// Called when the item is deleted.
    /// </summary>
    /// <param name="sender">
    /// The sender.
    /// </param>
    /// <param name="args">
    /// The arguments.
    /// </param>
    private void ItemDeletedNotification(object sender, ItemDeletedEventArgs args)
    {
      Assert.ArgumentNotNull(sender, "sender");
      Assert.ArgumentNotNull(args, "args");
      string list = (base.ServerProperties["deleted"] as string) ?? string.Empty;
      ListString listString = new ListString(list);
      foreach (string item in listString)
      {
        SheerResponse.Eval("scRemoveAlias(" + StringUtil.EscapeJavascriptString(StringUtil.EscapeQuote(item)) + ")");
      }
    }

    /// <summary>
    /// Refreshes the list.
    /// </summary>
    private void RefreshList()
    {
      Item itemFromQueryString = UIUtil.GetItemFromQueryString(Context.ContentDatabase);
      Error.AssertItemFound(itemFromQueryString);
      using (new SecurityDisabler())
      {
        List.Controls.Clear();
        Item item = Context.ContentDatabase.GetItem("/sitecore/system/Aliases");
        Error.AssertItemFound(item, "/sitecore/system/Aliases");
        Item[] descendants = item.Axes.GetDescendants();
        foreach (Item item2 in descendants)
        {
          LinkField linkField = item2.Fields["linked item"];
          if (linkField != null && linkField.TargetID == itemFromQueryString.ID)
          {
            ListItem listItem = new ListItem();
            List.Controls.Add(listItem);
            listItem.ID = "I" + item2.ID.ToShortID();
            listItem.Header = GetAliasPath(item2);
            listItem.Value = item2.ID.ToString();
          }
        }
      }
    }
  }
}
