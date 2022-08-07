using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace ArteHacker.UITKEditorAid
{
    /// <summary>
    /// A visual element for organizing content with tabs.
    /// It allows viewing multiple tabs at the same time when <see cref="allowMultipleSelection"/> is true;
    /// </summary>
    /// <example>
    /// A basic usage example with three tabs.
    /// <code>
    /// class ACustomEditor : Editor
    /// {
    ///     public override VisualElement CreateInspectorGUI()
    ///     {
    ///         var tabbedView = new TabbedView();
    ///         // Set allowMultipleSelection to true to view multiple tabs at the same time
    ///         // by holding shift or ctrl (cmd on macOS) when clicking on them.
    ///         tabbedView.allowMultipleSelection = true;
    /// 
    ///         tabbedView.AddTab(new Label("Tab 1"), new Label("Tab 1 Content"));
    ///         tabbedView.AddTab(new Label("Tab 2"), new Label("Tab 2 Content"));
    ///         tabbedView.AddTab(new Label("Tab 3"), new Label("Tab 3 Content"));
    /// 
    ///         // The first tab added is selected by default. This selects the last tab.
    ///         tabbedView.SetSelectedTab(2);
    /// 
    ///         return tabbedView;
    ///     }
    /// }
    /// </code>
    /// </example>
    public class TabbedView : VisualElement
    {
        private readonly struct TabPair
        {
            public readonly VisualElement tab;
            public readonly VisualElement content;

            public TabPair(VisualElement tab, VisualElement content)
            {
                this.tab = tab;
                this.content = content;
            }
        }

        /// <summary> USS class name of elements of this type. </summary>
        public static readonly string ussClassName = "editor-aid-tabbed-view";

        /// <summary> USS class name of the bar that contains all the tabs. </summary>
        public static readonly string tabBarUssClassName = ussClassName + "__tab-bar";
        /// <summary> USS class name of a tab element. </summary>
        public static readonly string tabUssClassName = ussClassName + "__tab";
        /// <summary> USS class name of a selected tab element. </summary>
        public static readonly string selectedTabUssClassName = tabUssClassName + "--selected";
        /// <summary> USS class name of a tab's title. </summary>
        public static readonly string tabTitleUssClassName = ussClassName + "__tab-title";

        /// <summary> USS class name of the element that contains all the tab contents. </summary>
        public static readonly string tabContentDisplayUssClassName = ussClassName + "__tab-content-display";
        /// <summary> USS class name a single tab's content. </summary>
        public static readonly string tabContentUssClassName = ussClassName + "__tab-content";

        private readonly List<TabPair> m_TabPairs = new List<TabPair>();
        private readonly VisualElement m_TabBar = new VisualElement();
        private readonly VisualElement m_TabContentDisplay = new VisualElement();

        /// <summary>
        /// When true, allows showing multiple tabs at the same time by holding shift or ctrl (or cmd in macOS) while clicking a tab.
        /// It's false by default.
        /// </summary>
        public bool allowMultipleSelection { get; set; }
        /// <summary> Gets the number of tabs that have been added. Can be used to know the index of the tab that will be added next. </summary>
        public int tabCount => m_TabPairs.Count;

        /// <summary> Event triggered when a tab's selection changed. Receives the tab's index and a bool indicating whether it's selected. </summary>
        public event System.Action<int, bool> onTabSelectionChange;

        /// <summary> TabbedView constructor. </summary>
        public TabbedView()
        {
            AddToClassList(ussClassName);
            EditorAidResources.ApplyCurrentTheme(this);
            styleSheets.Add(EditorAidResources.tabbedViewStyle);

            m_TabBar.AddToClassList(tabBarUssClassName);
            Add(m_TabBar);

            m_TabContentDisplay.AddToClassList(tabContentDisplayUssClassName);
            Add(m_TabContentDisplay);
        }

        /// <summary> Adds a tab and the content associated to it. </summary>
        /// <param name="title"> A title displayed in the tab. </param>
        /// <param name="content"> The content to be associated with the tab. </param>
        public void AddTab(VisualElement title, VisualElement content)
        {
            Assert.IsNotNull(content, "Tab content can't be null.");
            Assert.IsNotNull(title, "Tab title can't be null.");

            var tab = new VisualElement();
            tab.AddToClassList(tabUssClassName);
            tab.RegisterCallback<PointerDownEvent>(e => HandleTabSelection(e, tab));
            m_TabBar.Add(tab);

            title.AddToClassList(tabTitleUssClassName);
            tab.Add(title);

            content.AddToClassList(tabContentUssClassName);
            content.style.display = DisplayStyle.None;
            m_TabContentDisplay.Add(content);

            m_TabPairs.Add(new TabPair(tab, content));
            // Select tab if it's the first one.
            if (m_TabPairs.Count == 1)
                SetSelectedTab(tab);
        }

        /// <summary> Gets the tab content at the specified index. </summary>
        /// <param name="tabIndex"> Index of the tab. </param>
        /// <returns> The tab content. </returns>
        public VisualElement GetTabContent(int tabIndex)
        {
            return m_TabPairs[tabIndex].content;
        }

        /// <summary> Gets the tab title element at the specified index. </summary>
        /// <param name="tabIndex"> Index of the tab. </param>
        /// <returns> The tab title element. </returns>
        public VisualElement GetTabTitle(int tabIndex)
        {
            return m_TabPairs[tabIndex].tab?.Q();
        }

        /// <summary> Sets a single selected tab. </summary>
        /// <param name="tabIndex"> Index of the tab. </param>
        public void SetSelectedTab(int tabIndex)
        {
            SetSelectedTab(m_TabPairs[tabIndex].tab);
        }

        /// <summary> Selects a tab without unselecting others. </summary>
        /// <param name="tabIndex"> Index of the tab. </param>
        public void AddTabToSelection(int tabIndex)
        {
            AddTabToSelection(m_TabPairs[tabIndex].tab);
        }

        /// <summary> Unselects a tab. </summary>
        /// <param name="tabIndex"> Index of the tab. </param>
        public void RemoveTabFromSelection(int tabIndex)
        {
            RemoveTabFromSelection(m_TabPairs[tabIndex].tab);
        }

        private void HandleTabSelection(PointerDownEvent e, VisualElement tab)
        {
            if (allowMultipleSelection && (e.actionKey || e.shiftKey))
            {
                if (tab.ClassListContains(selectedTabUssClassName))
                    RemoveTabFromSelection(tab);
                else
                    AddTabToSelection(tab);
            }
            else
            {
                SetSelectedTab(tab);
            }

            e.StopPropagation();
        }

        private void AddTabToSelection(VisualElement tab)
        {
            for (int i = 0; i < m_TabPairs.Count; i++)
            {
                var pair = m_TabPairs[i];
                if (pair.tab != tab) continue;

                bool wasSelected = pair.tab.ClassListContains(selectedTabUssClassName);

                pair.tab.AddToClassList(selectedTabUssClassName);
                pair.content.style.display = DisplayStyle.Flex;

                if (!wasSelected)
                    onTabSelectionChange?.Invoke(i, true);
                break;
            }
        }

        private void RemoveTabFromSelection(VisualElement tab)
        {
            for (int i = 0; i < m_TabPairs.Count; i++)
            {
                TabPair pair = m_TabPairs[i];
                if (pair.tab != tab) continue;

                bool wasSelected = pair.tab.ClassListContains(selectedTabUssClassName);

                pair.tab.RemoveFromClassList(selectedTabUssClassName);
                pair.content.style.display = DisplayStyle.None;

                if (wasSelected)
                    onTabSelectionChange?.Invoke(i, false);
                break;
            }
        }

        private void SetSelectedTab(VisualElement tab)
        {
            for (int i = 0; i < m_TabPairs.Count; i++)
            {
                TabPair pair = m_TabPairs[i];
                bool shouldSelect = pair.tab == tab;
                bool wasSelected = pair.tab.ClassListContains(selectedTabUssClassName);

                pair.tab.EnableInClassList(selectedTabUssClassName, shouldSelect);
                pair.content.style.display = shouldSelect ? DisplayStyle.Flex : DisplayStyle.None;

                if (wasSelected != shouldSelect)
                    onTabSelectionChange?.Invoke(i, shouldSelect);
            }
        }
    }
}