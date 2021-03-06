﻿using System;
using Quark.Spells;
using Quark.Utilities;
using UnityEngine;

namespace Quark.Targeting
{
    public class TargetManager : Daemon<TargetManager>
    {
        public static LayerMask TerrainLayer = LayerMask.NameToLayer("Terrain");

        public static bool IsSelected
        {
            get { return SelectedCharacter != null; }
        }

        public static bool IsTargeting
        {
            get { return _runningMacro != null; }
        }

        public static bool IsBusy
        {
            get { return _runningMacro != null; }
        }

        public static Character SelectedCharacter { get; private set; }

        private static void OnCharacterClick(MouseArgs mouseArgs)
        {
            OnCharacterClick(mouseArgs.Character);
        }

        private static void OnCharacterClick(Character character)
        {
            if (character != null && !character.IsTargetable)
                return;
            Messenger.Broadcast("SelectedCharacterChanged");
            SelectedCharacter = character;
        }

        private static void OnPointClick(Vector3 point)
        {
            if (!IsTargeting)
                return;

            if (!IsPointValid)
            {
                Messenger<CastError>.Broadcast("CastError", new RangeError());
                return;
            }

            if (_pointCallback != null)
                _pointCallback(point);
        }

        public override void Update()
        {
            RunRaycast();
            if (Input.GetMouseButtonUp(0))
            {
                if (IsCharacterHovering)
                    OnCharacterClick(HoveringCharacter);
                else if (IsPointHovering)
                    OnPointClick(HoveringPoint);
                else
                    OnCharacterClick(null);
            }

            if (Input.GetMouseButton(1) && IsTargeting)
                _runningMacro.Cancel();

            if (Input.GetKeyUp(KeyCode.Escape))
                _runningMacro.Cancel();
        }

        static void RunRaycast()
        {
            HoveringCharacter = null;
            _hoveringPoint = null;

            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (IsTargeting)
            {
                if (Physics.Raycast(ray, out hit, Mathf.Infinity, 1 << TerrainLayer.value))
                    HoveringPoint = hit.point;
            }
            else if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.GetComponent<Character>() != null)
                    HoveringCharacter = hit.collider.GetComponent<Character>();
            }
        }

        public static Character HoveringCharacter
        {
            get;
            private set;
        }

        static Vector3? _hoveringPoint;

        public static Vector3 HoveringPoint
        {
            get
            {
                return (Vector3)_hoveringPoint;
            }
            private set
            {
                _hoveringPoint = value;
            }
        }

        public static bool IsCharacterHovering
        {
            get
            {
                return HoveringCharacter != null;
            }
        }

        public static bool IsPointHovering
        {
            get
            {
                return _hoveringPoint != null;
            }
        }

        public static float PointDistance
        {
            get
            {
                return Vector3.Distance(_runningMacro.Caster.transform.position, HoveringPoint);
            }
        }

        public static bool IsPointValid
        {
            get
            {
                if (!IsTargeting)
                    return false;

                if (!(_runningMacro is IRanged))
                    return true;

                if (PointDistance > ((IRanged)_runningMacro).CastRange)
                    return false;

                return true;
            }
        }

        public static bool ReserveTargeter(TargetMacro macro)
        {
            if (IsBusy)
                return false;

            _runningMacro = macro;
            return true;
        }

        public static void FreeTargeter()
        {
            _runningMacro = null;
            _pointCallback = null;
        }

        private static TargetMacro _runningMacro;
        private static Callback<Vector3> _pointCallback;

        public static void RequestPoint(Callback<Vector3> handler)
        {
            _pointCallback = handler;
        }

        public static Character RequestCharacter()
        {
            return SelectedCharacter;
        }
    }


    struct MouseArgs : IMessage
    {
        public Character Character { get; private set; }

        public Vector3 Point { get; private set; }

        public MouseEventType Type { get; private set; }

        public bool IsCharacter { get; private set; }

        public MouseArgs(Character character, MouseEventType type)
            : this()
        {
            Character = character;
            Type = type;
            Point = Vector3.zero;
            IsCharacter = true;
        }

        public MouseArgs(Vector3 point)
            : this()
        {
            Character = null;
            Point = point;
            IsCharacter = false;
            Type = MouseEventType.Click;
        }

        public void Broadcast()
        {
            Messenger<MouseArgs>.Broadcast("Mouse", this);
            Messenger<MouseArgs>.Broadcast(Type + ".Mouse", this);
            if (IsCharacter)
                Messenger<MouseArgs>.Broadcast(Character.Identifier + "." + Type + ".Mouse", this);
        }
    }

    enum MouseEventType
    {
        Enter,
        Exit,
        Click,
        Hover
    }

    public enum TargetType
    {
        None,
        Point,
        Targetable,
        Character
    }

    public enum TargetForm
    {
        /// <summary>
        /// May hit multiple characters
        /// </summary>
        Singular,
        /// <summary>
        /// May not hit multiple characters
        /// </summary>
        Plural
    }
}