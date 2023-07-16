using System.Linq;
using Content.Shared.PDA;
using Robust.Client.UserInterface.Controls;
using SharpFont;

namespace Content.Client.PDA.Messenger.CustomControls;

public sealed class PdaRecipientNameSection : BoxContainer
{
    private BoxContainer _Recipients;

    public PdaRecipientNameSection()
    {
        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;
        VerticalExpand = true;
        HorizontalAlignment = HAlignment.Center;

        AddChild(new Label()
        {
            StyleClasses = { "LabelBig" },
            Text = "Recipients",
            HorizontalExpand = true,
            HorizontalAlignment = HAlignment.Center,
        });

        _Recipients = new BoxContainer()
        {
            Orientation = LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
            HorizontalAlignment = HAlignment.Center,
        };

        AddChild(_Recipients);
    }

    public void PopulateRecipients(List<KnownPda> recipients)
    {
        _Recipients.DisposeAllChildren();
        foreach (var recipient in recipients)
        {
            var name = new RichTextLabel()
            {
                HorizontalExpand = true,
                HorizontalAlignment = HAlignment.Center,
            };
            name.SetMessage(recipient.Name);
            _Recipients.AddChild(name);
        }
    }


}
