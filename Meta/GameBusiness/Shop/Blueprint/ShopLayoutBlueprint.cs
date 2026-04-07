namespace GameBusiness.Shop.Blueprint
{
    using System;
    using System.Collections.Generic;
    using DataManager.Blueprint.BlueprintReader;

    [BlueprintReader("ShopLayout")]
    public class ShopLayoutBlueprint : GenericBlueprintReaderByRow<string, ShopLayoutRecord>
    {

    }

    [CsvHeaderKey("LayoutId")]
    public class ShopLayoutRecord
    {
        public string LayoutId;

        public BlueprintByRow<ShopSectionRecord> Sections;
    }

    [CsvHeaderKey("SectionId")]
    public class ShopSectionRecord
    {
        public string SectionId;
        public string SectionName;
        public int    SectionDisplayOrder;

        public Tuple<string, string> SectionTypeToPrefabView;
        public List<string>          SectionColorPalette; // List of color hex codes for the section theme, inorder of Header, Background

        public string       SectionCommonPackagePrefabView; // Common prefab for Packages in this section, can be overridden by Packages
        public List<string> SectionCommonPackageColorPalette; // Common color palette for Packages in this section, can be overridden by Packages, inorder of Header, Background, Bottom

        public BlueprintByRow<ShopPackageRecord> PackageRecords;
    }

    [CsvHeaderKey("PackageId")]
    public class ShopPackageRecord
    {
        public string PackageId;
        public int    ItemDisplayOrder;

        public string       OverrideIcon;
        public string       OverridePrefabView; // Override the section common prefab for this package
        public List<string> OverrideColorPalette; // Override the section common color palette for this package

        public string TagDescription;
        public string TagIconAssetPath;
    }
}
