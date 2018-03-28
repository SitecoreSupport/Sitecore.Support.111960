namespace Sitecore.Support.Shell.Applications.ContentManager.Dialogs.Aliases
{
  using Sitecore;
  using Sitecore.Configuration;
  using Sitecore.Data.Items;
  using Sitecore.Diagnostics;
  using Sitecore.Text;
  using Sitecore.Web.UI.HtmlControls;
  using Sitecore.Web.UI.Sheer;
  using System;
  using System.Collections.Generic;
  using System.Text.RegularExpressions;

  /// <summary>
  /// The aliases form.
  /// </summary>
  public class AliasesForm : Sitecore.Shell.Applications.ContentManager.Dialogs.Aliases.AliasesForm
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
          if (this._path.Count > 1)
          {
            for (int i = 0; i < this._path.Count - 1; i++)
            {
              yield return this._path[i];
            }
          }
        }
      }

      /// <summary>
      /// Gets the name of the ascenders and.
      /// </summary>
      /// <value>The name of the ascenders and.</value>
      public IEnumerable<string> AscendersAndName => this._path.Items;

      /// <summary>
      /// Gets the name.
      /// </summary>
      /// <value>The name.</value>
      public string Name => this._path[this._path.Count - 1];

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
        this._path = new ListString(value, '/');
      }
    }
   
    /// <summary>
    /// Handles a click on the Add button.
    /// </summary>
    protected new void Add_Click()
    {
      string value = this.NewAlias.Value;
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
        Item item = Context.ContentDatabase.GetItem("/sitecore/system/Aliases");
        Error.AssertItemFound(item, "/sitecore/system/Aliases");
        ListItem listItem = CreateAlias(aliasInfo, itemFromQueryString, item);
        if (listItem != null)
        {
          SheerResponse.Eval("scCreateAlias(" + StringUtil.EscapeJavascriptString(StringUtil.EscapeQuote(listItem.ID)) + ", " + StringUtil.EscapeJavascriptString(StringUtil.EscapeQuote(listItem.Header)) + ", " + StringUtil.EscapeJavascriptString(StringUtil.EscapeQuote(listItem.Value)) + ");");
          this.NewAlias.Value = string.Empty;
          SheerResponse.SetModified(false);
        }
      }
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
      ((BaseItem)item)["Linked Item"] = "<link linktype=\"internal\" url=\"" + target.Paths.ContentPath + "\" id=\"" + target.ID + "\" />";
      item.Editing.EndEdit();
      ListItem listItem = new ListItem();
      this.List.Controls.Add(listItem);
      listItem.ID = "I" + item.ID.ToShortID();
      listItem.Header = AliasesForm.GetAliasPath(item);
      listItem.Value = item.ID.ToString();
      return listItem;
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

  }
}