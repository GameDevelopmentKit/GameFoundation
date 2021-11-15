namespace vietlabs.fr2
{
    using UnityEditor;
    using UnityEngine;

    public class FR2_SearchView
    {
        private bool   caseSensitive;
        private string searchTerm = string.Empty;

        public static GUIStyle toolbarSearchField;
        public static GUIStyle toolbarSearchFieldCancelButton;
        public static GUIStyle toolbarSearchFieldCancelButtonEmpty;

        public static void InitSearchStyle()
        {
            toolbarSearchField                  = "ToolbarSeachTextFieldPopup";
            toolbarSearchFieldCancelButton      = "ToolbarSeachCancelButton";
            toolbarSearchFieldCancelButtonEmpty = "ToolbarSeachCancelButtonEmpty";
        }

        public bool DrawLayout()
        {
            var dirty = false;

            if (toolbarSearchField == null) InitSearchStyle();

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                var v = GUILayout.Toggle(this.caseSensitive, "Aa", EditorStyles.toolbarButton, GUILayout.Width(24f));
                if (v != this.caseSensitive)
                {
                    this.caseSensitive = v;
                    dirty              = true;
                }

                GUILayout.Space(2f);
                var value = GUILayout.TextField(this.searchTerm, toolbarSearchField, GUILayout.Width(140f));
                if (this.searchTerm != value)
                {
                    this.searchTerm = value;
                    dirty           = true;
                }

                var style = string.IsNullOrEmpty(this.searchTerm)
                    ? toolbarSearchFieldCancelButtonEmpty
                    : toolbarSearchFieldCancelButton;
                if (GUILayout.Button("Cancel", style))
                {
                    this.searchTerm = string.Empty;
                    dirty           = true;
                }
            }
            GUILayout.EndHorizontal();

            return dirty;
        }
    }
}