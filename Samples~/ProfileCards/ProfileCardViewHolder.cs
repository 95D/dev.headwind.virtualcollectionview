using UnityEngine;
using UnityEngine.UIElements;

namespace Headwind.VirtualCollectionView.Samples
{
    /// <summary>
    /// Holder that builds one profile card in code (no UXML assets) and binds <see cref="ProfileModel"/>s to it.
    /// </summary>
    /// <remarks>
    /// Instances are pooled by the view and rebound many times as the user
    /// scrolls.
    /// </remarks>
    public sealed class ProfileCardViewHolder : ViewHolder<ProfileModel>
    {
        private readonly VisualElement _avatar;
        private readonly Label _initial;
        private readonly Label _name;
        private readonly Label _title;
        private ProfileModel _bound;

        public ProfileCardViewHolder() : base(BuildCard())
        {
            _avatar = ItemView.Q<VisualElement>("avatar");
            _initial = ItemView.Q<Label>("initial");
            _name = ItemView.Q<Label>("name");
            _title = ItemView.Q<Label>("title");

            ItemView.RegisterCallback<ClickEvent>(_ =>
            {
                if (_bound != null)
                    Debug.Log($"Clicked profile #{_bound.Id}: {_bound.Name}");
            });
        }

        public override void Bind(ProfileModel item)
        {
            _bound = item;
            _avatar.style.backgroundColor = Color.HSVToRGB(item.Hue, 0.55f, 0.85f);
            _initial.text = item.Initial;
            _name.text = item.Name;
            _title.text = $"#{item.Id} · {item.Title}";
        }

        public override void OnRecycled() => _bound = null;

        private static VisualElement BuildCard()
        {
            var card = new VisualElement();
            card.style.flexDirection = FlexDirection.Row;
            card.style.alignItems = Align.Center;
            card.style.paddingLeft = 12f;
            card.style.paddingRight = 12f;
            card.style.backgroundColor = new Color(0.16f, 0.16f, 0.20f);
            card.style.borderTopLeftRadius = 8f;
            card.style.borderTopRightRadius = 8f;
            card.style.borderBottomLeftRadius = 8f;
            card.style.borderBottomRightRadius = 8f;

            var avatar = new VisualElement { name = "avatar" };
            avatar.style.width = 48f;
            avatar.style.height = 48f;
            avatar.style.borderTopLeftRadius = 24f;
            avatar.style.borderTopRightRadius = 24f;
            avatar.style.borderBottomLeftRadius = 24f;
            avatar.style.borderBottomRightRadius = 24f;
            avatar.style.justifyContent = Justify.Center;
            avatar.style.alignItems = Align.Center;
            avatar.style.flexShrink = 0f;
            card.Add(avatar);

            var initial = new Label { name = "initial" };
            initial.style.fontSize = 20f;
            initial.style.color = Color.white;
            initial.style.unityFontStyleAndWeight = FontStyle.Bold;
            avatar.Add(initial);

            var texts = new VisualElement();
            texts.style.marginLeft = 12f;
            texts.style.flexShrink = 1f;
            card.Add(texts);

            var name = new Label { name = "name" };
            name.style.fontSize = 14f;
            name.style.color = Color.white;
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            texts.Add(name);

            var title = new Label { name = "title" };
            title.style.fontSize = 12f;
            title.style.color = new Color(0.7f, 0.7f, 0.75f);
            texts.Add(title);

            return card;
        }
    }
}
