using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Timing;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Client.Info;

[GenerateTypedNameReferences]
public sealed partial class RulesPopup : Control
{
    [Dependency] private readonly IUriOpener _uri = default!; // DS14
    [Dependency] private readonly IConfigurationManager _cfg = default!; // DS14
    private float _timer;

    public float Timer
    {
        get => _timer;
        set
        {
            WaitLabel.Text = Loc.GetString("ui-rules-wait", ("time", MathF.Floor(value)));
            _timer = value;
        }
    }

    public event Action? OnQuitPressed;
    public event Action? OnAcceptPressed;

    public RulesPopup()
    {
        RobustXamlLoader.Load(this);

        AcceptButton.OnPressed += OnAcceptButtonPressed;
        QuitButton.OnPressed += OnQuitButtonPressed;

        // DS14-start
        WikiButton.OnPressed += _ =>
        {
            _uri.OpenUri(_cfg.GetCVar(CCVars.InfoLinksWiki));
        };
        WikiButton.Visible = !string.IsNullOrEmpty(_cfg.GetCVar(CCVars.InfoLinksWiki));
        // DS14-end
    }

    private void OnQuitButtonPressed(BaseButton.ButtonEventArgs obj)
    {
        OnQuitPressed?.Invoke();
    }

    private void OnAcceptButtonPressed(BaseButton.ButtonEventArgs obj)
    {
        OnAcceptPressed?.Invoke();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!AcceptButton.Disabled)
            return;

        if (Timer > 0.0)
        {
            if (Timer - args.DeltaSeconds < 0)
                Timer = 0;
            else
                Timer -= args.DeltaSeconds;
        }
        else
        {
            AcceptButton.Disabled = false;
        }
    }
}
