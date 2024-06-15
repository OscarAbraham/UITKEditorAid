using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace ArteHacker.UITKEditorAid
{
    /// <summary>
    /// Element for organizing content with tabs.
    /// </summary>
    /// <remarks>
    /// Set <see cref="allowMultipleSelection"/> true to support opening multiple tabs.
    /// Set <see cref="allowTabsOverflow"/> false to use multiple rows when tabs don't fit in a single line.
    /// Use the <see cref="AddTab(VisualElement, VisualElement)">AddTab</see> method that receives a VisualElement
    /// as a title if you need more than simple labels in the tabs. Pass a unique string to 
    /// <see cref="ApplyPersistenceKey(string)"/> to remember the last opened tabs.
    /// 
    /// </remarks>
    /// <example>
    /// A basic usage example with three tabs.
    /// <code language="csharp"><![CDATA[
    /// class ACustomEditor : Editor
    /// {
    ///     public override VisualElement CreateInspectorGUI()
    ///     {
    ///         var tabbedView = new TabbedView();
    ///         // Set allowMultipleSelection to view multiple tabs at the same time.
    ///         // It works by holding shift or ctrl (cmd on macOS) when clicking them.
    ///         tabbedView.allowMultipleSelection = true;
    /// 
    ///         // Use AddTab to create new tabs.
    ///         // The first parameter is an element used as the tab's title.
    ///         // The second parameter is an element used as the tab's content.
    ///         tabbedView.AddTab(new Label("Tab 0"), new Label("Tab 0 Content"));
    ///         tabbedView.AddTab(new Label("Tab 1"), new Label("Tab 1 Content"));
    ///         // You can use a string for the tab's title if you only need some text.
    ///         tabbedView.AddTab("Tab 2", new Label("Tab 2 Content"));
    /// 
    ///         // The first tab is selected by default. This selects Tab 2.
    ///         tabbedView.SetSelectedTab(2);
    ///         
    ///         // This selects Tab 0 and Tab 1 without unselecting any other tab.
    ///         tabbedView.AddTabToSelection(0);
    ///         tabbedView.AddTabToSelection(1);
    ///         
    ///         // This unselects Tab 2.
    ///         tabbedView.RemoveTabFromSelection(2);
    /// 
    ///         // Listen to this event to know when a tab's selection status changes.
    ///         tabbedView.onTabSelectionChange += (index, selected) =>
    ///         {
    ///             if (selected)
    ///                 Debug.Log($"Tab {index} selected");
    ///             else
    ///                 Debug.Log($"Tab {index} unselected");
    ///         };
    /// 
    ///         // Use a key to remember tab selection in views that use the same key.
    ///         tabbedView.ApplyPersistenceKey("ACustomEditor_TabsKey");
    /// 
    ///         return tabbedView;
    ///     }
    /// }
    /// ]]></code>
    /// </example>
    public class TabbedView : VisualElement
    {
        private class Tab : VisualElement
        {
            private int m_Index;
            private TabbedView m_View;

            public VisualElement content { get; private set; }

            public bool isSelected => ClassListContains(selectedTabUssClassName);

            public Tab(int index, TabbedView view, VisualElement content)
            {
                m_Index = index;
                m_View = view;
                this.content = content;

                AddToClassList(tabUssClassName);
                RegisterCallback<PointerDownEvent>(e => m_View.HandleTabSelection(e, m_Index));
            }

            public void Select()
            {
                AddToClassList(selectedTabUssClassName);
                content.style.display = DisplayStyle.Flex;
            }

            public void Unselect()
            {
                RemoveFromClassList(selectedTabUssClassName);
                content.style.display = DisplayStyle.None;
            }
        }

        /// <summary> USS class name of elements of this type. </summary>
        public static readonly string ussClassName = "editor-aid-tabbed-view";
        /// <summary> USS class name of Tabbed Views that allow tabs overflow. </summary>
        public static readonly string allowTabsOverflowUssClassName = ussClassName + "--allow-tabs-overflow";

        /// <summary> USS class name of the element that contains the tab bar. </summary>
        public static readonly string tabBarContainerUssClassName = ussClassName + "__tab-bar-container";
        /// <summary> USS class name of the bar that contains all the tabs. </summary>
        public static readonly string tabBarUssClassName = ussClassName + "__tab-bar";
        /// <summary> USS class name of the button to scroll tabs to the left when <see cref="allowTabsOverflow"/> is true. </summary>
        public static readonly string tabBarScrollLeftButtonUssClassName = ussClassName + "__tab-bar-scroll-left-button";
        /// <summary> USS class name of the button to scroll tabs to the right when <see cref="allowTabsOverflow"/> is true. </summary>
        public static readonly string tabBarScrollRightButtonUssClassName = ussClassName + "__tab-bar-scroll-right-button";

        /// <summary> USS class name of a tab element. </summary>
        public static readonly string tabUssClassName = ussClassName + "__tab";
        /// <summary> USS class name of a selected tab element. </summary>
        public static readonly string selectedTabUssClassName = tabUssClassName + "--selected";
        /// <summary> USS class name of a tab's title. </summary>
        public static readonly string tabTitleUssClassName = ussClassName + "__tab-title";

        /// <summary> USS class name of the element that contains all the tab contents. </summary>
        public static readonly string tabContentDisplayUssClassName = ussClassName + "__tab-content-display";
        /// <summary> USS class name of a single tab's content. </summary>
        public static readonly string tabContentUssClassName = ussClassName + "__tab-content";

        private readonly List<Tab> m_Tabs = new List<Tab>();
        private readonly VisualElement m_TabBarContainer = new VisualElement();
        private readonly VisualElement m_TabBar = new VisualElement { usageHints = UsageHints.DynamicTransform };
        private readonly RepeatButton m_LeftScrollButton = new RepeatButton { focusable = false, style = { display = DisplayStyle.None } };
        private readonly RepeatButton m_RightScrollButton = new RepeatButton { focusable = false, style = { display = DisplayStyle.None } };
        private readonly VisualElement m_TabContentDisplay = new VisualElement();

        private int m_PersistenceState = 1;
        private string m_PersistenceKey;

        /// <summary>
        /// When true, allows showing multiple tabs at the same time by holding shift or ctrl (or cmd in macOS) while clicking a tab.
        /// It's false by default.
        /// </summary>
        public bool allowMultipleSelection { get; set; }

        /// <summary>
        /// When true, the Tabbed View will keep tabs in a single row, clipping them when they don't fit and showing additional buttons to 
        /// scroll through them. When false, tabs that don't fit will be wrapped around, adding as many rows of tabs as necessary to fit them all.
        /// </summary>
        public bool allowTabsOverflow
        {
            get => ClassListContains(allowTabsOverflowUssClassName);
            set => EnableInClassList(allowTabsOverflowUssClassName, value);
        }

        /// <summary> The scroll speed for the tab bar when <see cref="allowTabsOverflow"/> is true. </summary>
        public float tabsScrollSpeed { get; set; } = 7;

        /// <summary> Gets the number of tabs that have been added. Can be used to know the index of the tab that will be added next. </summary>
        public int tabCount => m_Tabs.Count;

        /// <summary> Event triggered when a tab's selection changed. Receives the tab's index and a bool indicating whether it's selected. </summary>
        public event Action<int, bool> onTabSelectionChange;

        /// <summary> Constructor. </summary>
        public TabbedView()
        {
            AddToClassList(ussClassName);
            AddToClassList(allowTabsOverflowUssClassName);
            styleSheets.Add(EditorAidResources.tabbedViewStyle);

            m_TabBarContainer.AddToClassList(tabBarContainerUssClassName);
            Add(m_TabBarContainer);
            m_TabBar.AddToClassList(tabBarUssClassName);
            m_TabBarContainer.Add(m_TabBar);

            m_LeftScrollButton.AddToClassList(tabBarScrollLeftButtonUssClassName);
            m_TabBarContainer.Add(m_LeftScrollButton);
            m_LeftScrollButton.Add(new Image { scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore });
            m_LeftScrollButton.SetAction(ScrollTabsLeft, 0, 16);

            m_RightScrollButton.AddToClassList(tabBarScrollRightButtonUssClassName);
            m_TabBarContainer.Add(m_RightScrollButton);
            m_RightScrollButton.Add(new Image { scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore });
            m_RightScrollButton.SetAction(ScrollTabsRight, 0, 16);

            m_TabContentDisplay.AddToClassList(tabContentDisplayUssClassName);
            Add(m_TabContentDisplay);

            RegisterCallback<GeometryChangedEvent>(e =>
            {
                if (allowTabsOverflow)
                    UpdateScrollButtonsVisibility();
            });
        }

        /// <summary>
        /// Constructor. Receives persistence key to remember user selection of tabs. It's the same as calling
        /// <see cref="ApplyPersistenceKey(string)"/> after creating a TabbedView with the parameterless constructor.
        /// </summary>
        /// <param name="persistenceKey"></param>
        public TabbedView(string persistenceKey) : this()
        {
            ApplyPersistenceKey(persistenceKey);
        }

        /// <summary> Adds a tab and the content associated to it. </summary>
        /// <param name="title"> A title displayed in the tab. </param>
        /// <param name="content"> The content to be associated with the tab. </param>
        public void AddTab(string title, VisualElement content)
        {
            AddTab(new Label(title), content);
        }

        /// <summary> Adds a tab and the content associated to it. </summary>
        /// <param name="title"> A title displayed in the tab. </param>
        /// <param name="content"> The content to be associated with the tab. </param>
        public void AddTab(VisualElement title, VisualElement content)
        {
            Assert.IsNotNull(content, "Tab content can't be null.");
            Assert.IsNotNull(title, "Tab title can't be null.");

            content.AddToClassList(tabContentUssClassName);
            content.style.display = DisplayStyle.None;
            m_TabContentDisplay.Add(content);

            int newTabIndex = m_Tabs.Count;
            var tab = new Tab(newTabIndex, this, content);
            m_TabBar.Add(tab);
            m_Tabs.Add(tab);

            title.AddToClassList(tabTitleUssClassName);
            tab.Add(title);

            if (IsTabSelectedInPersistenceState(newTabIndex))
                AddTabToSelection(newTabIndex);
        }

        /// <summary> Gets the tab content at the specified index. </summary>
        /// <param name="tabIndex"> Index of the tab. </param>
        /// <returns> The tab content. </returns>
        public VisualElement GetTabContent(int tabIndex)
        {
            return m_Tabs[tabIndex].content;
        }

        /// <summary> Gets the tab title element at the specified index. </summary>
        /// <param name="tabIndex"> Index of the tab. </param>
        /// <returns> The tab title element. </returns>
        public VisualElement GetTabTitleElement(int tabIndex)
        {
            return m_Tabs[tabIndex]?.Q();
        }

        /// <summary>
        /// Use a persistence key to remember user selection of tabs. The selection is stored in <see cref="SessionState"/>.
        /// </summary>
        /// <param name="key"></param>
        public void ApplyPersistenceKey(string key)
        {
            m_PersistenceKey = key;
            if (string.IsNullOrEmpty(m_PersistenceKey))
                return;

            m_PersistenceState = SessionState.GetInt(m_PersistenceKey, m_PersistenceState);

            for (int i = 0; i < m_Tabs.Count; i++)
            {
                if (IsTabSelectedInPersistenceState(i))
                    AddTabToSelection(i);
                else
                    RemoveTabFromSelection(i);
            }
        }

        /// <summary> Check if a tab is selected. </summary>
        /// <param name="tabIndex"> Index of the tab. </param>
        /// <returns> Whether the tab is selected </returns>
        public bool IsTabSelected(int tabIndex)
        {
            return m_Tabs[tabIndex].isSelected;
        }

        /// <summary> Sets a single selected tab. </summary>
        /// <param name="tabIndex"> Index of the tab. </param>
        public void SetSelectedTab(int tabIndex)
        {
            for (int i = 0; i < m_Tabs.Count; i++)
            {
                if (i == tabIndex)
                    AddTabToSelection(i);
                else
                    RemoveTabFromSelection(i);
            }
        }

        /// <summary> Selects a tab without unselecting others. </summary>
        /// <param name="tabIndex"> Index of the tab. </param>
        public void AddTabToSelection(int tabIndex)
        {
            var tab = m_Tabs[tabIndex];
            if (tab.isSelected)
                return;

            tab.Select();
            SelectTabInPersistenceState(tabIndex);
            onTabSelectionChange?.Invoke(tabIndex, true);
        }

        /// <summary> Unselects a tab. </summary>
        /// <param name="tabIndex"> Index of the tab. </param>
        public void RemoveTabFromSelection(int tabIndex)
        {
            var tab = m_Tabs[tabIndex];
            if (!tab.isSelected)
                return;

            tab.Unselect();
            UnselectTabInPersistenceState(tabIndex);
            onTabSelectionChange?.Invoke(tabIndex, false);
        }

        private void HandleTabSelection(PointerDownEvent e, int index)
        {
            if (allowMultipleSelection && (e.actionKey || e.shiftKey))
            {
                if (IsTabSelected(index))
                    RemoveTabFromSelection(index);
                else
                    AddTabToSelection(index);
            }
            else
            {
                SetSelectedTab(index);
            }

            FlushPersistenceState();
            e.StopPropagation();
        }

        private bool IsTabSelectedInPersistenceState(int tabIndex)
        {
            // Our persistence state is an int, so we can't store more than 32 tabs. 
            return tabIndex < 32 && (m_PersistenceState & (1 << tabIndex)) != 0;
        }

        private void SelectTabInPersistenceState(int tabIndex)
        {
            if (tabIndex < 32)
                m_PersistenceState |= 1 << tabIndex;
        }

        private void UnselectTabInPersistenceState(int tabIndex)
        {
            if (tabIndex < 32)
                m_PersistenceState &= ~(1 << tabIndex);
        }

        private void FlushPersistenceState()
        {
            if (string.IsNullOrEmpty(m_PersistenceKey))
                return;
            SessionState.SetInt(m_PersistenceKey, m_PersistenceState);
        }

        private void ScrollTabsLeft()
        {
            var tabBarRect = m_TabBar.worldBound;
            var tabBarContainerRect = m_TabBarContainer.worldBound;
            float scrollAmount = Mathf.Max(0, Mathf.Min(tabsScrollSpeed, tabBarContainerRect.xMin - tabBarRect.xMin));

            m_TabBar.transform.position = m_TabBar.transform.position + (Vector3.right * scrollAmount);

            UpdateScrollButtonsVisibility();
        }

        private void ScrollTabsRight()
        {
            var tabBarRect = m_TabBar.worldBound;
            var tabBarContainerRect = m_TabBarContainer.worldBound;
            float scrollAmount = Mathf.Max(0, Mathf.Min(tabsScrollSpeed, tabBarRect.xMax - tabBarContainerRect.xMax));

            m_TabBar.transform.position = m_TabBar.transform.position + (Vector3.left * scrollAmount);

            UpdateScrollButtonsVisibility();
        }

        private void UpdateScrollButtonsVisibility()
        {
            var tabBarRect = m_TabBar.worldBound;
            var tabBarContainerRect = m_TabBarContainer.worldBound;

            m_LeftScrollButton.style.display = tabBarRect.xMin < tabBarContainerRect.xMin ? DisplayStyle.Flex : DisplayStyle.None;
            m_RightScrollButton.style.display = tabBarRect.xMax > tabBarContainerRect.xMax ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}