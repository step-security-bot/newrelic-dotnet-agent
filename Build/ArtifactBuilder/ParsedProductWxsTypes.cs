﻿using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace ArtifactBuilder
{
	// This code was auto-generated by visual studio and then edited to remove all of the generated
	// classes that were not needed for our testing purposes.

	// NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
	/// <remarks />
	[Serializable]
	[DesignerCategory("code")]
	[XmlType(AnonymousType = true, Namespace = "http://schemas.microsoft.com/wix/2006/wi")]
	[XmlRoot(Namespace = "http://schemas.microsoft.com/wix/2006/wi", IsNullable = false)]
	public class Wix
	{
		/// <remarks />
		public WixFragment Fragment { get; set; }
	}

	/// <remarks />
	[Serializable]
	[DesignerCategory("code")]
	[XmlType(AnonymousType = true, Namespace = "http://schemas.microsoft.com/wix/2006/wi")]
	public class WixFragment
	{
		/// <remarks />
		[XmlElement("ComponentGroup", typeof(WixFragmentComponentGroup))]
		public object[] Items { get; set; }
	}

	/// <remarks />
	[Serializable]
	[DesignerCategory("code")]
	[XmlType(AnonymousType = true, Namespace = "http://schemas.microsoft.com/wix/2006/wi")]
	public class WixFragmentComponentGroup
	{
		/// <remarks />
		[XmlElement("Component")]
		public WixFragmentComponentGroupComponent[] Component { get; set; }

		/// <remarks />
		[XmlAttribute]
		public string Id { get; set; }

		/// <remarks />
		[XmlAttribute]
		public string Directory { get; set; }
	}

	/// <remarks />
	[Serializable]
	[DesignerCategory("code")]
	[XmlType(AnonymousType = true, Namespace = "http://schemas.microsoft.com/wix/2006/wi")]
	public class WixFragmentComponentGroupComponent
	{
		/// <remarks />
		public WixFragmentComponentGroupComponentFile File { get; set; }
	}

	/// <remarks />
	[Serializable]
	[DesignerCategory("code")]
	[XmlType(AnonymousType = true, Namespace = "http://schemas.microsoft.com/wix/2006/wi")]
	public class WixFragmentComponentGroupComponentFile
	{
		/// <remarks />
		[XmlAttribute]
		public string Id { get; set; }

		/// <remarks />
		[XmlAttribute]
		public string Name { get; set; }

		/// <remarks />
		[XmlAttribute]
		public string KeyPath { get; set; }

		/// <remarks />
		[XmlAttribute]
		public string Source { get; set; }
	}
}