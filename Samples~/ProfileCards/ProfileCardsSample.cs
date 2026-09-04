using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Headwind.VirtualCollectionView.Core;

namespace Headwind.VirtualCollectionView.Samples
{
    /// <summary>
    /// Sample behaviour to attach to a GameObject with a UIDocument.
    /// </summary>
    /// <remarks>
    /// Builds a toolbar that swaps column/row/grid layout managers over one
    /// shared adapter of 10,000 profile cards, plus shuffle / scroll-to-random
    /// actions.
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public sealed class ProfileCardsSample : MonoBehaviour
    {
        private static readonly string[] FirstNames =
        {
            "Aria", "Bruno", "Chae", "Dana", "Emre", "Fumi", "Gwan", "Hana",
            "Ivo", "Jun", "Kai", "Lena", "Min", "Noor", "Olan", "Priya",
            "Quinn", "Rae", "Sana", "Tomo"
        };

        private static readonly string[] LastNames =
        {
            "Ahn", "Baek", "Cho", "Doyle", "Endo", "Fuse", "Gray", "Han",
            "Ito", "Jang", "Kim", "Lee", "Mori", "Nam", "Oh", "Park",
            "Ryu", "Seo", "Tanaka", "Yun"
        };

        private static readonly string[] Titles =
        {
            "Client Engineer", "Server Engineer", "Tech Artist", "Producer",
            "Game Designer", "QA Engineer", "Data Analyst", "UX Designer"
        };

        [SerializeField] private int itemCount = 10000;

        private VirtualCollectionView<ProfileModel> _view;
        private ProfileCardAdapter _adapter;
        private readonly System.Random _random = new System.Random(42);
        private int _nextId;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear();
            root.style.flexGrow = 1f;
            root.style.backgroundColor = new Color(0.10f, 0.10f, 0.12f);

            _adapter = new ProfileCardAdapter(GenerateProfiles(itemCount));

            _view = new VirtualCollectionView<ProfileModel>();
            _view.style.flexGrow = 1f;
            _view.ItemAnimator = new DefaultItemAnimator<ProfileModel>();
            _view.SetLayoutManager(new ColumnLayoutManager { Spacing = 8f, Padding = 12f });
            _view.SetAdapter(_adapter);

            root.Add(BuildToolbar());
            root.Add(_view);
        }

        private VisualElement BuildToolbar()
        {
            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.flexShrink = 0f;
            toolbar.style.paddingLeft = 12f;
            toolbar.style.paddingTop = 8f;
            toolbar.style.paddingBottom = 8f;

            toolbar.Add(new Button(() =>
                _view.SetLayoutManager(new ColumnLayoutManager { Spacing = 8f, Padding = 12f }))
            { text = "Column" });

            toolbar.Add(new Button(() =>
                _view.SetLayoutManager(new RowLayoutManager { Spacing = 8f, Padding = 12f }))
            { text = "Row" });

            toolbar.Add(new Button(() =>
                _view.SetLayoutManager(new GridLayoutManager(3)
                {
                    MainSpacing = 8f,
                    CrossSpacing = 8f,
                    Padding = 12f
                }))
            { text = "Grid ×3" });

            toolbar.Add(new Button(() => _adapter.Shuffle(_random)) { text = "Shuffle" });

            toolbar.Add(new Button(() => _view.ScrollToItem(_random.Next(_adapter.Count)))
            { text = "Jump" });

            // Granular changes animate (scroll to the top to watch them).
            toolbar.Add(new Button(() => _adapter.InsertAt(2, CreateProfile()))
            { text = "+ Insert@2" });

            toolbar.Add(new Button(() =>
            {
                if (_adapter.Count > 2)
                    _adapter.RemoveAt(2);
            })
            { text = "− Remove@2" });

            toolbar.Add(new Button(() =>
            {
                if (_adapter.Count > 6)
                    _adapter.Move(2, 6);
            })
            { text = "Move 2→6" });

            var count = new Label($"{itemCount:N0} items");
            count.style.alignSelf = Align.Center;
            count.style.marginLeft = 8f;
            count.style.color = new Color(0.7f, 0.7f, 0.75f);
            toolbar.Add(count);

            return toolbar;
        }

        private List<ProfileModel> GenerateProfiles(int count)
        {
            var list = new List<ProfileModel>(count);
            for (var i = 0; i < count; i++)
                list.Add(CreateProfile());

            return list;
        }

        private ProfileModel CreateProfile()
        {
            var name = $"{FirstNames[_random.Next(FirstNames.Length)]} {LastNames[_random.Next(LastNames.Length)]}";
            var title = Titles[_random.Next(Titles.Length)];
            return new ProfileModel(_nextId++, name, title, (float)_random.NextDouble());
        }
    }
}
