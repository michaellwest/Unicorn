﻿using System;
using System.Linq;
using Sitecore.Data;
using Sitecore.Data.Events;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;

namespace Unicorn.Data
{
	public class SitecoreSourceItem : ISourceItem
	{
		private readonly Item _item;

		public SitecoreSourceItem(Item item)
		{
			Assert.IsNotNull(item, "item");

			_item = item;
		}

		public string Name
		{
			get { return _item.Name; }
		}

		public string ItemPath
		{
			get
			{
				// note: using Path here instead of FullPath because when you get a _renamed_ item instance, the FullPath points to the _old_ path, whereas Path points to the correct, new path.
				return _item.Paths.Path;
			}
		}

		public string DatabaseName
		{
			get { return _item.Database.Name; }
		}

		public ID Id
		{
			get { return _item.ID; }
		}

		public string TemplateName
		{
			get { return _item.TemplateName; }
		}

		public ID TemplateId
		{
			get { return _item.TemplateID; }
		}

		public string DisplayIdentifier
		{
			get { return string.Format("{0}:{1} ({2})", DatabaseName, ItemPath, Id); }
		}

		public Item InnerItem { get { return _item; } }

		/// <summary>
		/// Recycles the item, clears it from cache, and if it's part of a template resets the template engine
		/// </summary>
		public void Recycle()
		{
			var database = _item.Database;
			var itemId = _item.ID;

			_item.Recycle();

			if (EventDisabler.IsActive)
			{
				database.Caches.ItemCache.RemoveItem(itemId);
				database.Caches.DataCache.RemoveItemInformation(itemId);
			}

			if (_item.Database.Engines.TemplateEngine.IsTemplatePart(_item))
				database.Engines.TemplateEngine.Reset();
		}


		public ISourceItem[] Children
		{
			get { return _item.Children.Select(x => (ISourceItem)new SitecoreSourceItem(x)).ToArray(); }
		}

		public DateTime? GetLastModifiedDate(string languageCode, int versionNumber)
		{
			var version = GetVersion(languageCode, versionNumber);

			if (version == null) return null;

			return version.Statistics.Updated;
		}

		public string GetRevision(string languageCode, int versionNumber)
		{
			var version = GetVersion(languageCode, versionNumber);

			if (version == null) return null;

			return version.Statistics.Revision;
		}

		private Item GetVersion(string languageCode, int versionNumber)
		{
			Assert.ArgumentNotNullOrEmpty(languageCode, "languageCode");

			Language language;

			if (!Language.TryParse(languageCode, out language)) throw new ArgumentOutOfRangeException("languageCode", "Language code was not valid!");

			var version = new Sitecore.Data.Version(versionNumber);

			return _item.Database.GetItem(_item.ID, language, version);
		}
	}
}
