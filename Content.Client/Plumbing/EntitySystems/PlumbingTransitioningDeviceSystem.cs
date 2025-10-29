// SPDX-FileCopyrightText: 2025 LaCumbiaDelCoronavirus
//
// SPDX-License-Identifier: MPL-2.0

using Content.Shared.Plumbing.Components;
using Content.Shared.Plumbing.EntitySystems;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.Plumbing.EntitySystems;

public sealed class PlumbingTransitioningDeviceSystem : SharedPlumbingTransitioningDeviceSystem
{
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;
    [Dependency] private readonly AnimationPlayerSystem _animationSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlumbingTransitioningDeviceComponent, ComponentStartup>(OnDeviceStartup);

        SubscribeLocalEvent<PlumbingTransitioningDeviceComponent, AppearanceChangeEvent>(OnAppearanceChange);
        //SubscribeLocalEvent<PlumbingTransitioningDeviceComponent, AnimationCompletedEvent>(OnAnimationComplete);
    }

    private void OnDeviceStartup(Entity<PlumbingTransitioningDeviceComponent> entity, ref ComponentStartup args)
    {
        var component = entity.Comp;

        if (component.ToOffState != null)
            component.ToOffAnimation = new Animation
            {
                Length = component.OffLength,
                AnimationTracks = {
                    new AnimationTrackSpriteFlick() {
                        LayerKey = component.Layer,
                        KeyFrames = {new AnimationTrackSpriteFlick.KeyFrame(component.ToOffState, 0f)}
                    },
                }
            };

        if (component.ToOnState != null)
            component.ToOnAnimation = new Animation
            {
                Length = component.OnLength,
                AnimationTracks = {
                    new AnimationTrackSpriteFlick() {
                        LayerKey = component.Layer,
                        KeyFrames = {new AnimationTrackSpriteFlick.KeyFrame(component.ToOnState, 0f)}
                    },
                }
            };

        UpdateAppearance(entity, component.State, null, null);
    }

    private void OnAnimationComplete(Entity<PlumbingTransitioningDeviceComponent> entity, ref AnimationCompletedEvent args)
    {
        if (args.Key != PlumbingTransitioningDeviceComponent.AnimationKey)
            return;

        var deviceComponent = entity.Comp;

        Log.Debug($"New state after anim: {deviceComponent.State + 1}");
        UpdateAppearance(entity, deviceComponent.State + 1, args.AnimationPlayer, null);
    }

    private void UpdateAppearance(Entity<PlumbingTransitioningDeviceComponent> entity, PlumbingDeviceState state, AnimationPlayerComponent? animationPlayerComponent = null, SpriteComponent? spriteComponent = null)
    {
        if (!Resolve(entity.Owner, ref animationPlayerComponent, logMissing: false) ||
            !Resolve(entity.Owner, ref spriteComponent, logMissing: false))
            return;

        UpdateIcon(entity, state, animationPlayerComponent, spriteComponent);
    }

    /// <summary>
    ///     Updates the entity's sprite to match it's current VisualState.
    /// </summary>
    private void UpdateIcon(Entity<PlumbingTransitioningDeviceComponent> entity, PlumbingDeviceState state, AnimationPlayerComponent animationPlayerComponent, SpriteComponent spriteComponent)
    {
        if (_animationSystem.HasRunningAnimation(animationPlayerComponent, PlumbingTransitioningDeviceComponent.AnimationKey))
            _animationSystem.Stop((entity.Owner, animationPlayerComponent), PlumbingTransitioningDeviceComponent.AnimationKey);

        var transitioningDeviceComponent = entity.Comp;
        switch (state)
        {
            case PlumbingDeviceState.ToOff:
                if (transitioningDeviceComponent.ToOffAnimation != null)
                    _animationSystem.Play((entity.Owner, animationPlayerComponent), (Animation) transitioningDeviceComponent.ToOffAnimation, PlumbingTransitioningDeviceComponent.AnimationKey);

                break;
            case PlumbingDeviceState.ToOn:
                if (transitioningDeviceComponent.ToOnAnimation != null)
                    _animationSystem.Play((entity.Owner, animationPlayerComponent), (Animation) transitioningDeviceComponent.ToOnAnimation, PlumbingTransitioningDeviceComponent.AnimationKey);

                break;
            case PlumbingDeviceState.Off:
                _spriteSystem.LayerSetRsiState((entity.Owner, spriteComponent), transitioningDeviceComponent.Layer, transitioningDeviceComponent.OffState);
                break;
            case PlumbingDeviceState.On:
                _spriteSystem.LayerSetRsiState((entity.Owner, spriteComponent), transitioningDeviceComponent.Layer, transitioningDeviceComponent.OnState);
                break;
        }
    }

    private void OnAppearanceChange(Entity<PlumbingTransitioningDeviceComponent> entity, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } spriteComponent)
            return;

        if (!AppearanceSystem.TryGetData<PlumbingDeviceState>(entity, PlumbingDeviceVisuals.State, out var state, args.Component))
            state = PlumbingDeviceState.Off;

        UpdateAppearance(entity, state, null, spriteComponent);
    }
}
