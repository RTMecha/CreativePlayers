using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

using InControl;
using XInputDotNetPure;
using TMPro;
using DG.Tweening;

using LSFunctions;

using CreativePlayers.Functions;
using CreativePlayers.Functions.Components;
using CreativePlayers.Functions.Data;

using RTFunctions.Functions;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;

namespace CreativePlayers
{
    public class RTPlayer : MonoBehaviour
    {
        //Player Parent Tree (original):
        //player-complete (has Player component)
        //player-complete/Player
        //player-complete/Player/Player (has OnTriggerEnterPass component)
        //player-complete/Player/Player/death-explosion
        //player-complete/Player/Player/burst-explosion
        //player-complete/Player/Player/spawn-implosion
        //player-complete/Player/boost
        //player-complete/trail (has PlayerTrail component)
        //player-complete/trail/1
        //player-complete/trail/2
        //player-complete/trail/3

        #region Base

        public static RTPlayer GetInstance(int index)
        {
            if (InputDataManager.inst.players.Count < 1 || InputDataManager.inst.players.Count <= index || InputDataManager.inst.players[index].GetRTPlayer() == null)
                return null;

            return InputDataManager.inst.players[index].GetRTPlayer();
        }

        public MyGameActions Actions { get; set; }

        public FaceController faceController;

        public int playerIndex;

        public int initialHealthCount;

        public Coroutine boostCoroutine;

        public GameObject canvas;

        public TextMeshPro textMesh;
        public MeshRenderer healthBase;

        public GameObject health;

        private RectTransform barRT;
        private Image barIm;
        private Image barBaseIm;

        #endregion

        #region Bool

        public bool canBoost = true;
        public bool canMove = true;
        public bool canTakeDamage;

        public bool isTakingHit;
        public bool isBoosting;
        public bool isBoostCancelled;
        public bool isDead = true;

        public bool isKeyboard;
        private bool animatingBoost;

        #endregion

        #region Velocities

        private Vector3 lastPos;
        private float lastMoveHorizontal;
        private float lastMoveVertical;
        private Vector3 lastVelocity;

        public Vector2 lastMovement;

        private float startHurtTime;
        private float startBoostTime;
        public float maxBoostTime = 0.18f;
        public float minBoostTime = 0.07f;
        public float boostCooldown = 0.1f;
        public float idleSpeed = 20f;
        public float boostSpeed = 85f;

        public bool includeNegativeZoom = false;
        public MovementMode movementMode = 0;

        public RotateMode rotateMode = RotateMode.RotateToDirection;

        private Vector2 lastMousePos;

        public bool stretch = true;
        public float stretchAmount = 0.1f;
        public int stretchEasing = 6;
        public Vector2 stretchVector = Vector2.zero;

        #endregion

        #region Enums

        public enum RotateMode
        {
            RotateToDirection,
            None,
            FlipX,
            FlipY
        }

        public enum MovementMode
        {
            KeyboardController,
            Mouse
        }

        #endregion

        #region Tail

        public bool tailGrows = false;
        public bool boostTail = false;
        public float tailDistance = 2f;
        public int tailMode;
        public TailUpdateMode updateMode = TailUpdateMode.FixedUpdate;
        public enum TailUpdateMode
        {
            Update,
            FixedUpdate,
            LateUpdate
        }

        #endregion

        #region Properties

        public InputDataManager.CustomPlayer CustomPlayer => InputDataManager.inst.players[playerIndex];

        public bool CanTakeDamage
        {
            get => (!(EditorManager.inst == null) || DataManager.inst.GetSettingEnum("ArcadeDifficulty", 1) != 0 || !DataManager.inst.GetSettingBool("IsArcade")) && (!(EditorManager.inst == null) || GameManager.inst.gameState != GameManager.State.Paused) && (!(EditorManager.inst != null) || !EditorManager.inst.isEditing) && canTakeDamage;
            set => canTakeDamage = value;
        }

        public bool CanMove
        {
            get => canMove;
            set => canMove = value;
        }

        public bool CanBoost
        {
            get => (!(EditorManager.inst != null) || !EditorManager.inst.isEditing) && (canBoost && !isBoosting) && (GameManager.inst == null || GameManager.inst.gameState != GameManager.State.Paused) && !LSHelpers.IsUsingInputField();
            set => canBoost = value;
        }

        public bool PlayerAlive => (!(InputDataManager.inst != null) || InputDataManager.inst.players.Count > 0) && (InputDataManager.inst != null && InputDataManager.inst.players[playerIndex].health > 0) && !isDead;

        #endregion

        #region Delegates

        public delegate void PlayerHitDelegate(int _health, Vector3 _pos);

        public delegate void PlayerHealDelegate(int _health, Vector3 _pos);

        public delegate void PlayerBoostDelegate();

        public delegate void PlayerDeathDelegate(Vector3 _pos);

        public event PlayerHitDelegate playerHitEvent;

        public event PlayerHealDelegate playerHealEvent;

        public event PlayerBoostDelegate playerBoostEvent;

        public event PlayerDeathDelegate playerDeathEvent;

        #endregion

        #region Spawn

        void Awake()
        {
            //if (gameObject.GetComponent<Player>())
            //    Destroy(gameObject.GetComponent<Player>());
            if (transform.Find("trail").GetComponent<PlayerTrail>())
                Destroy(transform.Find("trail").GetComponent<PlayerTrail>());

            playerObjects.Add("Base", new PlayerObject("Base", gameObject));
            playerObjects["Base"].values.Add("Transform", gameObject.transform);
            var anim = gameObject.GetComponent<Animator>();
            anim.keepAnimatorControllerStateOnDisable = true;
            playerObjects["Base"].values.Add("Animator", anim);

            var rb = transform.Find("Player").gameObject;
            playerObjects.Add("RB Parent", new PlayerObject("RB Parent", rb));
            playerObjects["RB Parent"].values.Add("Transform", rb.transform);
            playerObjects["RB Parent"].values.Add("Rigidbody2D", rb.GetComponent<Rigidbody2D>());
            playerObjects["RB Parent"].values.Add("OnTriggerEnterPass", rb.GetComponent<OnTriggerEnterPass>());

            var circleCollider = rb.GetComponent<CircleCollider2D>();

            circleCollider.enabled = false;

            var polygonCollider = rb.AddComponent<PolygonCollider2D>();

            playerObjects["RB Parent"].values.Add("CircleCollider2D", circleCollider);
            playerObjects["RB Parent"].values.Add("PolygonCollider2D", polygonCollider);
            playerObjects["RB Parent"].values.Add("PlayerSelector", rb.AddComponent<PlayerSelector>());
            ((PlayerSelector)playerObjects["RB Parent"].values["PlayerSelector"]).id = playerIndex;

            var head = transform.Find("Player/Player").gameObject;
            playerObjects.Add("Head", new PlayerObject("Head", head));

            var headMesh = head.GetComponent<MeshFilter>();

            playerObjects["Head"].values.Add("MeshFilter", headMesh);

            polygonCollider.CreateCollider(headMesh);

            playerObjects["Head"].values.Add("MeshRenderer", head.GetComponent<MeshRenderer>());

            polygonCollider.isTrigger = EditorManager.inst != null && PlayerPlugin.ZenEditorIncludesSolid.Value && PlayerPlugin.ZenModeInEditor.Value;
            polygonCollider.enabled = false;
            circleCollider.enabled = true;

            circleCollider.isTrigger = EditorManager.inst != null && PlayerPlugin.ZenEditorIncludesSolid.Value && PlayerPlugin.ZenModeInEditor.Value;
            rb.GetComponent<Rigidbody2D>().collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            try
            {
                if (head.GetComponent<OnTriggerEnterPass>())
                {
                    Destroy(head.GetComponent<OnTriggerEnterPass>());

                    var playerCollision = head.AddComponent<PlayerCollision>();
                    playerCollision.player = this;
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"{PlayerPlugin.className}Error!\nEXCEPTION: {ex.Message}\nSTACKTRACE: {ex.StackTrace}");
            }

            var boost = transform.Find("Player/boost").gameObject;
            boost.transform.localScale = Vector3.zero;
            playerObjects.Add("Boost", new PlayerObject("Boost", transform.Find("Player/boost").gameObject));
            playerObjects["Boost"].values.Add("MeshFilter", boost.GetComponent<MeshFilter>());
            playerObjects["Boost"].values.Add("MeshRenderer", boost.GetComponent<MeshRenderer>());

            playerObjects.Add("Tail Parent", new PlayerObject("Tail Parent", transform.Find("trail").gameObject));
            var tail1 = transform.Find("trail/1").gameObject;
            playerObjects.Add("Tail 1", new PlayerObject("Tail 1", tail1));
            var tail2 = transform.Find("trail/2").gameObject;
            playerObjects.Add("Tail 2", new PlayerObject("Tail 2", tail2));
            var tail3 = transform.Find("trail/3").gameObject;
            playerObjects.Add("Tail 3", new PlayerObject("Tail 3", tail3));

            playerObjects["Tail 1"].values.Add("MeshFilter", tail1.GetComponent<MeshFilter>());
            playerObjects["Tail 2"].values.Add("MeshFilter", tail2.GetComponent<MeshFilter>());
            playerObjects["Tail 3"].values.Add("MeshFilter", tail3.GetComponent<MeshFilter>());
            playerObjects["Tail 1"].values.Add("MeshRenderer", tail1.GetComponent<MeshRenderer>());
            playerObjects["Tail 2"].values.Add("MeshRenderer", tail2.GetComponent<MeshRenderer>());
            playerObjects["Tail 3"].values.Add("MeshRenderer", tail3.GetComponent<MeshRenderer>());
            playerObjects["Tail 1"].values.Add("TrailRenderer", tail1.GetComponent<TrailRenderer>());
            playerObjects["Tail 2"].values.Add("TrailRenderer", tail2.GetComponent<TrailRenderer>());
            playerObjects["Tail 3"].values.Add("TrailRenderer", tail3.GetComponent<TrailRenderer>());

            tail1.transform.localPosition = new Vector3(0f, 0f, 0.1f);
            tail2.transform.localPosition = new Vector3(0f, 0f, 0.1f);
            tail3.transform.localPosition = new Vector3(0f, 0f, 0.1f);

            //Set new parents
            {
                Debug.LogFormat("{0}Moving Player Objects to new parents so we can have them moved / scaled / rotated without other main stuff getting in the way.", PlayerPlugin.className);
                //Tail 1
                GameObject trail1Base = new GameObject("Tail 1 Base");
                trail1Base.layer = 8;
                trail1Base.transform.SetParent(transform.Find("trail"));
                transform.Find("trail/1").SetParent(trail1Base.transform);

                playerObjects.Add("Tail 1 Base", new PlayerObject("Tail 1 Base", trail1Base));

                //Tail 2
                GameObject trail2Base = new GameObject("Tail 2 Base");
                trail2Base.layer = 8;
                trail2Base.transform.SetParent(transform.Find("trail"));
                transform.Find("trail/2").SetParent(trail2Base.transform);

                playerObjects.Add("Tail 2 Base", new PlayerObject("Tail 2 Base", trail2Base));

                //Tail 3
                GameObject trail3Base = new GameObject("Tail 3 Base");
                trail3Base.layer = 8;
                trail3Base.transform.SetParent(transform.Find("trail"));
                transform.Find("trail/3").SetParent(trail3Base.transform);

                playerObjects.Add("Tail 3 Base", new PlayerObject("Tail 3 Base", trail3Base));

                //Boost
                GameObject boostBase = new GameObject("Boost Base");
                boostBase.layer = 8;
                boostBase.transform.SetParent(transform.Find("Player"));
                boost.transform.SetParent(boostBase.transform);
                boost.transform.localPosition = Vector3.zero;
                boost.transform.localRotation = Quaternion.identity;

                playerObjects.Add("Boost Base", new PlayerObject("Boost Base", boostBase));

                var boostTail = Instantiate(boostBase);
                boostTail.name = "Boost Tail";
                boostTail.layer = 8;
                boostTail.transform.SetParent(transform.Find("trail"));

                playerObjects.Add("Boost Tail Base", new PlayerObject("Boost Tail Base", boostTail));
                var child = boostTail.transform.GetChild(0);

                bool showBoost = this.boostTail;

                playerObjects.Add("Boost Tail", new PlayerObject("Boost Tail", child.gameObject));
                playerObjects["Boost Tail"].values.Add("MeshRenderer", child.GetComponent<MeshRenderer>());
                playerObjects["Boost Tail"].values.Add("MeshFilter", child.GetComponent<MeshFilter>());

                path.Add(new MovementPath(Vector3.zero, Quaternion.identity, rb.transform));
                path.Add(new MovementPath(Vector3.zero, Quaternion.identity, boostTail.transform, showBoost));
                path.Add(new MovementPath(Vector3.zero, Quaternion.identity, trail1Base.transform));
                path.Add(new MovementPath(Vector3.zero, Quaternion.identity, trail2Base.transform));
                path.Add(new MovementPath(Vector3.zero, Quaternion.identity, trail3Base.transform));
                path.Add(new MovementPath(Vector3.zero, Quaternion.identity, null));
            }

            //Add new stuff
            {
                GameObject delayTarget = new GameObject("tail-tracker");
                delayTarget.transform.SetParent(rb.transform);
                delayTarget.transform.localPosition = new Vector3(-0.5f, 0f, 0.1f);
                delayTarget.transform.localRotation = Quaternion.identity;
                playerObjects.Add("Tail Tracker", new PlayerObject("Tail Tracker", delayTarget));

                var faceBase = new GameObject("face-base");
                faceBase.transform.SetParent(rb.transform);
                faceBase.transform.localPosition = Vector3.zero;
                faceBase.transform.localScale = Vector3.one;
                
                playerObjects.Add("Face Base", new PlayerObject("Face Base", faceBase));

                var faceParent = new GameObject("face-parent");
                faceParent.transform.SetParent(faceBase.transform);
                faceParent.transform.localPosition = Vector3.zero;
                faceParent.transform.localScale = Vector3.one;
                faceParent.transform.localRotation = Quaternion.identity;
                playerObjects.Add("Face Parent", new PlayerObject("Face Parent", faceParent));

                //DelayTracker
                var boostDelay = playerObjects["Boost Tail Base"].gameObject.AddComponent<DelayTracker>();
                boostDelay.leader = delayTarget.transform;
                boostDelay.player = this;
                playerObjects["Boost Tail Base"].values.Add("DelayTracker", boostDelay);

                for (int i = 1; i < 4; i++)
                {
                    var tail = playerObjects[string.Format("Tail {0} Base", i)].gameObject;
                    var delayTracker = tail.AddComponent<DelayTracker>();
                    delayTracker.offset = -i * tailDistance / 2f;
                    delayTracker.positionOffset *= (-i + 4);
                    delayTracker.player = this;
                    delayTracker.leader = delayTarget.transform;
                    playerObjects[string.Format("Tail {0} Base", i)].values.Add("DelayTracker", delayTracker);
                }

                var mat = head.transform.Find("death-explosion").GetComponent<ParticleSystemRenderer>().trailMaterial;

                //Trail
                {
                    GameObject superTrail = new GameObject("super-trail");
                    superTrail.transform.SetParent(head.transform);
                    superTrail.layer = 8;

                    var trailRenderer = superTrail.AddComponent<TrailRenderer>();

                    playerObjects.Add("Head Trail", new PlayerObject("Head Trail", superTrail));
                    playerObjects["Head Trail"].values.Add("TrailRenderer", trailRenderer);

                    trailRenderer.material = mat;
                }

                //Particles
                {
                    GameObject superParticles = new GameObject("super-particles");
                    superParticles.transform.SetParent(head.transform);
                    superParticles.layer = 8;

                    var particleSystem = superParticles.AddComponent<ParticleSystem>();
                    if (!superParticles.GetComponent<ParticleSystemRenderer>())
                    {
                        superParticles.AddComponent<ParticleSystemRenderer>();
                    }

                    var particleSystemRenderer = superParticles.GetComponent<ParticleSystemRenderer>();

                    playerObjects.Add("Head Particles", new PlayerObject("Head Particles", superParticles));
                    playerObjects["Head Particles"].values.Add("ParticleSystem", particleSystem);
                    playerObjects["Head Particles"].values.Add("ParticleSystemRenderer", particleSystemRenderer);

                    var main = particleSystem.main;
                    main.simulationSpace = ParticleSystemSimulationSpace.World;
                    main.playOnAwake = false;
                    particleSystemRenderer.renderMode = ParticleSystemRenderMode.Mesh;
                    particleSystemRenderer.alignment = ParticleSystemRenderSpace.View;

                    particleSystemRenderer.trailMaterial = mat;
                    particleSystemRenderer.material = mat;
                }

                //Trail
                {
                    GameObject superTrail = new GameObject("boost-trail");
                    superTrail.transform.SetParent(boost.transform.parent);
                    superTrail.layer = 8;

                    var trailRenderer = superTrail.AddComponent<TrailRenderer>();

                    playerObjects.Add("Boost Trail", new PlayerObject("Boost Trail", superTrail));
                    playerObjects["Boost Trail"].values.Add("TrailRenderer", trailRenderer);

                    trailRenderer.material = mat;
                }

                //Boost Particles
                {
                    GameObject superParticles = new GameObject("boost-particles");
                    superParticles.transform.SetParent(boost.transform.parent);
                    superParticles.layer = 8;

                    var particleSystem = superParticles.AddComponent<ParticleSystem>();
                    if (!superParticles.GetComponent<ParticleSystemRenderer>())
                    {
                        superParticles.AddComponent<ParticleSystemRenderer>();
                    }

                    var particleSystemRenderer = superParticles.GetComponent<ParticleSystemRenderer>();

                    playerObjects.Add("Boost Particles", new PlayerObject("Boost Particles", superParticles));
                    playerObjects["Boost Particles"].values.Add("ParticleSystem", particleSystem);
                    playerObjects["Boost Particles"].values.Add("ParticleSystemRenderer", particleSystemRenderer);

                    var main = particleSystem.main;
                    main.simulationSpace = ParticleSystemSimulationSpace.World;
                    main.loop = false;
                    main.playOnAwake = false;
                    particleSystemRenderer.renderMode = ParticleSystemRenderMode.Mesh;
                    particleSystemRenderer.alignment = ParticleSystemRenderSpace.View;

                    particleSystemRenderer.trailMaterial = mat;
                    particleSystemRenderer.material = mat;
                }

                //Tail Particles
                {
                    for (int i = 1; i < 4; i++)
                    {
                        GameObject superParticles = new GameObject("tail-particles");
                        superParticles.transform.SetParent(playerObjects[string.Format("Tail {0} Base", i)].gameObject.transform);
                        superParticles.layer = 8;

                        var particleSystem = superParticles.AddComponent<ParticleSystem>();
                        if (!superParticles.GetComponent<ParticleSystemRenderer>())
                        {
                            superParticles.AddComponent<ParticleSystemRenderer>();
                        }

                        var particleSystemRenderer = superParticles.GetComponent<ParticleSystemRenderer>();

                        playerObjects.Add(string.Format("Tail {0} Particles", i), new PlayerObject(string.Format("Tail {0} Particles", i), superParticles));
                        playerObjects[string.Format("Tail {0} Particles", i)].values.Add("ParticleSystem", particleSystem);
                        playerObjects[string.Format("Tail {0} Particles", i)].values.Add("ParticleSystemRenderer", particleSystemRenderer);

                        var main = particleSystem.main;
                        main.simulationSpace = ParticleSystemSimulationSpace.World;
                        main.playOnAwake = false;
                        particleSystemRenderer.renderMode = ParticleSystemRenderMode.Mesh;
                        particleSystemRenderer.alignment = ParticleSystemRenderSpace.View;

                        particleSystemRenderer.trailMaterial = mat;
                        particleSystemRenderer.material = mat;
                    }
                }
            }

            health = Instantiate(PlayerPlugin.healthImages);
            health.transform.SetParent(PlayerPlugin.healthParent);
            health.transform.localScale = Vector3.one;
            health.name = "Health " + playerIndex.ToString();

            for (int i = 0; i < 3; i++)
            {
                healthObjects.Add(new HealthObject(health.transform.GetChild(i).gameObject, health.transform.GetChild(i).GetComponent<Image>()));
            }

            var barBase = new GameObject("Bar Base");
            barBase.transform.SetParent(health.transform);
            barBase.transform.localScale = Vector3.one;

            var barBaseRT = barBase.AddComponent<RectTransform>();
            var barBaseLE = barBase.AddComponent<LayoutElement>();
            barBaseIm = barBase.AddComponent<Image>();

            barBaseLE.ignoreLayout = true;
            barBaseRT.anchoredPosition = new Vector2(-100f, 0f);
            barBaseRT.pivot = new Vector2(0f, 0.5f);
            barBaseRT.sizeDelta = new Vector2(200f, 32f);

            var bar = new GameObject("Bar");
            bar.transform.SetParent(barBase.transform);
            bar.transform.localScale = Vector3.one;

            barRT = bar.AddComponent<RectTransform>();
            barIm = bar.AddComponent<Image>();

            barRT.pivot = new Vector2(0f, 0.5f);
            barRT.anchoredPosition = new Vector2(-100f, 0f);
        }

        void Start()
        {
            playerHealEvent += UpdateTail;
            playerHitEvent += UpdateTail;
            SetColor(LSColors.red500, LSColors.gray900);
            Spawn();
        }

        void Spawn()
        {
            //var anim = (Animator)playerObjects["Base"].values["Animator"];
            var currentModel = PlayerPlugin.CurrentModel(playerIndex);
            var rb = (Rigidbody2D)playerObjects["RB Parent"].values["Rigidbody2D"];
            if (EditorManager.inst == null && DataManager.inst.GetSettingEnum("ArcadeDifficulty", 0) == 3)
            {
                InputDataManager.inst.players[playerIndex].health = 1;
                UpdateTail(InputDataManager.inst.players[playerIndex].health, rb.position);
            }
            else
            {
                if (currentModel != null)
                {
                    InputDataManager.inst.players[playerIndex].health = (int)currentModel.values["Base Health"];
                    UpdateTail(InputDataManager.inst.players[playerIndex].health, rb.position);
                }
                else
                {
                    InputDataManager.inst.players[playerIndex].health = 3;
                }
            }
            CanTakeDamage = false;
            CanBoost = false;
            CanMove = false;
            isDead = false;
            isBoosting = false;
            ((Animator)playerObjects["Base"].values["Animator"]).SetTrigger("spawn");
            PlaySpawnParticles();
            SetBindings(Actions);

            EvaluateSpawnCode();

            Debug.LogFormat("{0}Spawned Player {1}", PlayerPlugin.className, playerIndex);
        }

        MyGameActions SetBindings(MyGameActions _actions)
        {
            if (EditorManager.inst != null && InputDataManager.inst.players.Count < 2)
            {
                MyGameActions myGameActions = _actions;
                myGameActions.Up.AddDefaultBinding(InputControlType.DPadUp);
                myGameActions.Up.AddDefaultBinding(InputControlType.LeftStickUp);
                myGameActions.Down.AddDefaultBinding(InputControlType.DPadDown);
                myGameActions.Down.AddDefaultBinding(InputControlType.LeftStickDown);
                myGameActions.Left.AddDefaultBinding(InputControlType.DPadLeft);
                myGameActions.Left.AddDefaultBinding(InputControlType.LeftStickLeft);
                myGameActions.Right.AddDefaultBinding(InputControlType.DPadRight);
                myGameActions.Right.AddDefaultBinding(InputControlType.LeftStickRight);
                //myGameActions.Boost.AddDefaultBinding(InputControlType.RightTrigger);
                //myGameActions.Boost.AddDefaultBinding(InputControlType.RightBumper);
                myGameActions.Boost.AddDefaultBinding(InputControlType.Action1);
                myGameActions.Boost.AddDefaultBinding(InputControlType.Action3);
                //myGameActions.Join.AddDefaultBinding(InputControlType.Action1);
                //myGameActions.Join.AddDefaultBinding(InputControlType.Action2);
                //myGameActions.Join.AddDefaultBinding(InputControlType.Action3);
                //myGameActions.Join.AddDefaultBinding(InputControlType.Action4);
                myGameActions.Pause.AddDefaultBinding(InputControlType.Command);
                myGameActions.Escape.AddDefaultBinding(InputControlType.Action2);
                myGameActions.Escape.AddDefaultBinding(InputControlType.Action4);
                return myGameActions;
            }
            else if (CustomPlayer.device == null)
            {
                MyGameActions myGameActions = _actions;

                foreach (var action in myGameActions.Actions)
                {
                    for (int i = 0; i < action.Bindings.Count; i++)
                    {
                        if (action.Bindings[i].BindingSourceType == BindingSourceType.DeviceBindingSource)
                            action.RemoveBindingAt(i);
                    }
                }

                return myGameActions;
            }

            return _actions;
        }

        #endregion

        #region Update Methods

        void Update()
        {
            UpdateCustomTheme(); UpdateBoostTheme(); UpdateSpeeds(); UpdateTrailLengths();
            if (canvas != null)
            {
                bool act = InputDataManager.inst.players.Count > 1 && PlayerPlugin.PlayerNameTags.Value;
                canvas.SetActive(act);

                if (act && textMesh != null)
                {
                    textMesh.text = "<#" + LSColors.ColorToHex(GameManager.inst.LiveTheme.playerColors[playerIndex % 4]) + ">Player " + (playerIndex + 1).ToString() + " " + PlayerExtensions.ConvertHealthToEquals(CustomPlayer.health, initialHealthCount);
                    healthBase.material.color = LSColors.fadeColor(GameManager.inst.LiveTheme.playerColors[playerIndex % 4], 0.3f);
                    healthBase.transform.localScale = new Vector3((float)initialHealthCount * 2.25f, 1.5f, 1f);
                }
            }

            //Anim
            {
                if (GameManager.inst.gameState == GameManager.State.Paused)
                {
                    ((Animator)playerObjects["Base"].values["Animator"]).speed = 0f;
                }
                else if (GameManager.inst.gameState == GameManager.State.Playing)
                {
                    ((Animator)playerObjects["Base"].values["Animator"]).speed = 1f / PlayerExtensions.Pitch;
                }
            }

            if (updateMode == TailUpdateMode.Update)
            {
                UpdateTailDistance();
            }

            var currentModel = PlayerPlugin.CurrentModel(playerIndex);
            if (playerObjects["Boost Trail"].values["TrailRenderer"] != null && currentModel != null && (bool)currentModel.values["Boost Trail Emitting"])
            {
                var tf = playerObjects["Boost"].gameObject.transform;
                Vector2 v = new Vector2(tf.localScale.x, tf.localScale.y);

                ((TrailRenderer)playerObjects["Boost Trail"].values["TrailRenderer"]).startWidth = (float)currentModel.values["Boost Trail Start Width"] * v.magnitude / 1.414213f;
                ((TrailRenderer)playerObjects["Boost Trail"].values["TrailRenderer"]).endWidth = (float)currentModel.values["Boost Trail End Width"] * v.magnitude / 1.414213f;
            }

            if (GameManager.inst == null || GameManager.inst.gameState != GameManager.State.Paused)
            {
                if (!PlayerAlive && !isDead)
                {
                    StartCoroutine(Kill());
                }
                if (CanMove && PlayerAlive && Actions != null)
                {
                    if (Actions.Boost.WasPressed && CanBoost)
                    {
                        StartBoost();
                        return;
                    }
                    if (isBoosting && !isBoostCancelled && (Actions.Boost.WasReleased || startBoostTime + maxBoostTime <= Time.time))
                    {
                        InitMidBoost(true);
                    }
                }

                if (PlayerAlive && faceController != null && currentModel.values.ContainsKey("Bullet Constant"))
                {
                    if (!(bool)currentModel.values["Bullet Constant"] && faceController.Shoot.WasPressed && canShoot)
                    {
                        CreateBullet();
                    }
                    if ((bool)currentModel.values["Bullet Constant"] && faceController.Shoot.IsPressed && canShoot)
                    {
                        CreateBullet();
                    }
                }

                if (Input.GetKeyDown(KeyCode.F5))
                    updatePlayer();
            }
        }

        bool canShoot = true;

        void FixedUpdate()
        {
            if (updateMode == TailUpdateMode.FixedUpdate)
            {
                UpdateTailDistance();
            }

            health.SetActive(GameManager.inst.timeline.activeSelf);

            if (InputDataManager.inst.players[playerIndex].health > path.Count - 2 && tailGrows)
            {
                var t = Instantiate(path[path.Count - 2].transform.gameObject);
                t.transform.SetParent(playerObjects["Tail Parent"].gameObject.transform);
                t.transform.localScale = Vector3.one;
                t.name = string.Format("Tail {0}", path.Count - 2);

                path.Insert(path.Count - 2, new MovementPath(t.transform.localPosition, t.transform.localRotation, t.transform));
            }
        }

        void LateUpdate()
        {
            if (updateMode == TailUpdateMode.LateUpdate)
            {
                UpdateTailDistance();
            }
            UpdateTailTransform(); UpdateTailDev(); UpdateTailSizes();

            var rb = (Rigidbody2D)playerObjects["RB Parent"].values["Rigidbody2D"];
            var anim = (Animator)playerObjects["Base"].values["Animator"];
            var player = playerObjects["RB Parent"].gameObject;
            var currentModel = PlayerPlugin.CurrentModel(playerIndex);

            if (PlayerAlive && Actions != null && InputDataManager.inst.players[playerIndex].active && CanMove && (GameManager.inst == null || GameManager.inst.gameState != GameManager.State.Paused) && !LSHelpers.IsUsingInputField() && movementMode == MovementMode.KeyboardController && (!ModCompatibility.sharedFunctions.ContainsKey("EventsCoreEditorOffset") || !(bool)ModCompatibility.sharedFunctions["EventsCoreEditorOffset"]))
            {
                //if (Actions.Escape.WasPressed && EditorManager.inst != null && !EditorManager.inst.isEditing)
                //{
                //    EditorManager.inst.ToggleEditor();
                //}

                float x = Actions.Move.Vector.x;
                float y = Actions.Move.Vector.y;
                if (x != 0f)
                {
                    lastMoveHorizontal = x;
                    if (y == 0f)
                    {
                        lastMoveVertical = 0f;
                    }
                }
                if (y != 0f)
                {
                    lastMoveVertical = y;
                    if (x == 0f)
                    {
                        lastMoveHorizontal = 0f;
                    }
                }

                float pitch = PlayerExtensions.Pitch;

                Vector3 vector = Vector3.zero;
                if (isBoosting)
                {
                    vector = new Vector3(lastMoveHorizontal, lastMoveVertical, 0f);
                    vector = vector.normalized;

                    rb.velocity = vector * boostSpeed * pitch;
                    if (stretch && rb.velocity.magnitude > 0f)
                    {
                        float e = 1f + rb.velocity.magnitude * stretchAmount / 20f;
                        player.transform.localScale = new Vector3(1f * e + stretchVector.x, 1f / e + stretchVector.y, 1f);
                    }
                }
                else
                {
                    vector = new Vector3(x, y, 0f);
                    if (vector.magnitude > 1f)
                    {
                        vector = vector.normalized;
                    }

                    var sp = 1f;
                    if ((bool)currentModel.values["Base Sprint Sneak Active"])
                    {
                        if (isKeyboard && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && !(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) || Input.GetKey(GetKeyCode(99)))
                        {
                            sp = 0.1f;
                        }
                        if (isKeyboard && !(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) || Input.GetKey(GetKeyCode(95)))
                        {
                            sp = 1.3f;
                        }
                    }

                    rb.velocity = vector * idleSpeed * pitch * sp;
                    if (stretch && rb.velocity.magnitude > 0f)
                    {
                        if (rotateMode != RotateMode.None && rotateMode != RotateMode.FlipX)
                        {
                            float e = 1f + rb.velocity.magnitude * stretchAmount / 20f;
                            player.transform.localScale = new Vector3(1f * e + stretchVector.x, 1f / e + stretchVector.y, 1f);
                        }
                        if (rotateMode == RotateMode.None || rotateMode == RotateMode.FlipX || rotateMode == RotateMode.FlipY)
                        {
                            float e = 1f + rb.velocity.magnitude * stretchAmount / 20f;

                            float xm = lastMoveHorizontal;
                            if (xm > 0f)
                                xm = -xm;

                            float ym = lastMoveVertical;
                            if (ym > 0f)
                                ym = -ym;

                            float xt = 1f * e + ym + stretchVector.x;
                            float yt = 1f * e + xm + stretchVector.y;

                            if (rotateMode == RotateMode.FlipX)
                            {
                                if (lastMovement.x > 0f)
                                    player.transform.localScale = new Vector3(xt, yt, 1f);
                                if (lastMovement.x < 0f)
                                    player.transform.localScale = new Vector3(-xt, yt, 1f);
                            }
                            if (rotateMode == RotateMode.FlipY)
                            {
                                if (lastMovement.y > 0f)
                                    player.transform.localScale = new Vector3(xt, yt, 1f);
                                if (lastMovement.y < 0f)
                                    player.transform.localScale = new Vector3(xt, -yt, 1f);
                            }
                            if (rotateMode == RotateMode.None)
                            {
                                player.transform.localScale = new Vector3(xt, yt, 1f);
                            }
                        }
                    }
                    else if (stretch)
                    {
                        float xt = 1f + stretchVector.x;
                        float yt = 1f + stretchVector.y;

                        if (rotateMode == RotateMode.FlipX)
                        {
                            if (lastMovement.x > 0f)
                                player.transform.localScale = new Vector3(xt, yt, 1f);
                            if (lastMovement.x < 0f)
                                player.transform.localScale = new Vector3(-xt, yt, 1f);
                        }
                        if (rotateMode == RotateMode.FlipY)
                        {
                            if (lastMovement.y > 0f)
                                player.transform.localScale = new Vector3(xt, yt, 1f);
                            if (lastMovement.y < 0f)
                                player.transform.localScale = new Vector3(xt, -yt, 1f);
                        }
                        if (rotateMode == RotateMode.None)
                        {
                            player.transform.localScale = new Vector3(xt, yt, 1f);
                        }
                    }
                }
                anim.SetFloat("Speed", Mathf.Abs(vector.x + vector.y));
                //Consider moving this outside to test.
                //Quaternion b = Quaternion.AngleAxis(Mathf.Atan2(lastVelocity.y, lastVelocity.x) * 57.29578f, player.transform.forward);
                //player.transform.rotation = Quaternion.Slerp(player.transform.rotation, b, 720f * Time.deltaTime);
                //player.transform.eulerAngles = new Vector3(0f, 0f, player.transform.eulerAngles.z);

                if (rb.velocity != Vector2.zero)
                {
                    lastVelocity = rb.velocity;
                }

                //player.transform.rotation = Quaternion.Euler(0f, 0f, player.transform.rotation.eulerAngles.z);
            }
            else if (CanMove)
            {
                rb.velocity = Vector3.zero;
            }

            if (PlayerAlive && InputDataManager.inst.players[playerIndex].active && CanMove && (GameManager.inst == null || GameManager.inst.gameState != GameManager.State.Paused) && !LSHelpers.IsUsingInputField() && movementMode == MovementMode.Mouse && (EditorManager.inst == null || !EditorManager.inst.isEditing) && Application.isFocused && isKeyboard && (!ModCompatibility.sharedFunctions.ContainsKey("EventsCoreEditorOffset") || !(bool)ModCompatibility.sharedFunctions["EventsCoreEditorOffset"]))
            {
                Vector2 screenCenter = new Vector2(1920 / 2 * (int)EditorManager.inst.ScreenScale, 1080 / 2 * (int)EditorManager.inst.ScreenScale);
                Vector2 mousePos = new Vector2(System.Windows.Forms.Cursor.Position.X - screenCenter.x, -(System.Windows.Forms.Cursor.Position.Y - (screenCenter.y * 2)) - screenCenter.y);

                if (lastMousePos != new Vector2(System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y))
                {
                    System.Windows.Forms.Cursor.Position = new System.Drawing.Point((int)screenCenter.x, (int)screenCenter.y);
                }

                var mousePosition = Input.mousePosition;
                mousePosition = Camera.main.WorldToScreenPoint(mousePosition);

                float num = idleSpeed * 0.00025f;
                if (isBoosting)
                    num = boostSpeed * 0.0001f;

                //player.transform.position += new Vector3(mousePos.x * num, mousePos.y * num, 0f);
                player.transform.localPosition = new Vector3(mousePosition.x, mousePosition.y, 0f);
                lastMousePos = new Vector2(mousePosition.x, mousePosition.y);
            }

            if ((!ModCompatibility.sharedFunctions.ContainsKey("EventsCoreEditorOffset") || !(bool)ModCompatibility.sharedFunctions["EventsCoreEditorOffset"]) && GameManager.inst.gameState == GameManager.State.Playing)
            {
                Vector3 vector2 = Camera.main.WorldToViewportPoint(player.transform.position);
                vector2.x = Mathf.Clamp(vector2.x, 0f, 1f);
                vector2.y = Mathf.Clamp(vector2.y, 0f, 1f);
                if (Camera.main.orthographicSize > 0f && (!includeNegativeZoom || Camera.main.orthographicSize < 0f))
                {
                    float maxDistanceDelta = Time.deltaTime * 1500f;
                    player.transform.position = Vector3.MoveTowards(lastPos, Camera.main.ViewportToWorldPoint(vector2), maxDistanceDelta);
                }
            }

            if ((bool)currentModel.values["Face Control Active"] && faceController != null)
            {
                var vector = new Vector2(faceController.Move.Vector.x, faceController.Move.Vector.y);
                var fp = (Vector2)currentModel.values["Face Position"];
                if (vector.magnitude > 1f)
                {
                    vector = vector.normalized;
                }

                if (rotateMode == RotateMode.FlipX && lastMovement.x < 0f)
                    vector.x = -vector.x;
                if (rotateMode == RotateMode.FlipY && lastMovement.y < 0f)
                    vector.y = -vector.y;

                playerObjects["Face Parent"].gameObject.transform.localPosition = new Vector3(vector.x * 0.3f + fp.x, vector.y * 0.3f + fp.y, 0f);
            }

            if (rotateMode != RotateMode.RotateToDirection)
            {
                Quaternion b = Quaternion.AngleAxis(Mathf.Atan2(lastMovement.y, lastMovement.x) * 57.29578f, player.transform.forward);

                var c = Quaternion.Slerp(player.transform.rotation, b, 720f * Time.deltaTime).eulerAngles;

                if (rotateMode == RotateMode.FlipX && c.z > 90f && c.z < 270f)
                {
                    c.z = -c.z + 180f;
                }
                if (rotateMode == RotateMode.FlipY && c.z > 0f && c.z < 180f)
                {
                    c.z = -c.z + 90f;
                }

                playerObjects["Face Base"].gameObject.transform.rotation = Quaternion.Euler(c);
            }

            if (rotateMode == RotateMode.RotateToDirection)
            {
                Quaternion b = Quaternion.AngleAxis(Mathf.Atan2(lastMovement.y, lastMovement.x) * 57.29578f, player.transform.forward);
                player.transform.rotation = Quaternion.Slerp(player.transform.rotation, b, 720f * Time.deltaTime);

                playerObjects["Face Base"].gameObject.transform.localRotation = Quaternion.identity;
            }

            if (rotateMode == RotateMode.FlipX)
            {
                player.transform.rotation = Quaternion.identity;
                if (lastMovement.x > 0.001f)
                {
                    if (!stretch)
                        player.transform.localScale = Vector3.one;
                    if (!animatingBoost)
                        playerObjects["Boost Tail Base"].gameObject.transform.localScale = Vector3.one;
                    playerObjects["Tail 1 Base"].gameObject.transform.localScale = Vector3.one;
                    playerObjects["Tail 2 Base"].gameObject.transform.localScale = Vector3.one;
                    playerObjects["Tail 3 Base"].gameObject.transform.localScale = Vector3.one;
                }
                if (lastMovement.x < -0.001f)
                {
                    var c = new Vector3(-1f, 1f, 1f);
                    if (!stretch)
                        player.transform.localScale = c;
                    if (!animatingBoost)
                        playerObjects["Boost Tail Base"].gameObject.transform.localScale = c;
                    playerObjects["Tail 1 Base"].gameObject.transform.localScale = c;
                    playerObjects["Tail 2 Base"].gameObject.transform.localScale = c;
                    playerObjects["Tail 3 Base"].gameObject.transform.localScale = c;
                }
            }
            else if (rotateMode == RotateMode.FlipY)
            {
                player.transform.rotation = Quaternion.identity;
                if (lastMovement.y > 0.001f)
                {
                    float x = player.transform.localScale.x;
                    float y = player.transform.localScale.y;

                    if (x < 0f)
                    {
                        x = -x;
                    }
                    if (y < 0f)
                    {
                        y = -y;
                    }

                    if (!stretch)
                        player.transform.localScale = Vector3.one;
                    if (!animatingBoost)
                        playerObjects["Boost Tail Base"].gameObject.transform.localScale = Vector3.one;
                    playerObjects["Tail 1 Base"].gameObject.transform.localScale = Vector3.one;
                    playerObjects["Tail 2 Base"].gameObject.transform.localScale = Vector3.one;
                    playerObjects["Tail 3 Base"].gameObject.transform.localScale = Vector3.one;
                }
                if (lastMovement.y < -0.001f)
                {
                    var c = new Vector3(1f, -1f, 1f);
                    if (!stretch)
                        player.transform.localScale = c;
                    if (!animatingBoost)
                        playerObjects["Boost Tail Base"].gameObject.transform.localScale = c;
                    playerObjects["Tail 1 Base"].gameObject.transform.localScale = c;
                    playerObjects["Tail 2 Base"].gameObject.transform.localScale = c;
                    playerObjects["Tail 3 Base"].gameObject.transform.localScale = c;
                }
            }

            if (rotateMode == RotateMode.None)
            {
                player.transform.rotation = Quaternion.identity;
            }

            //var posCalc = (player.transform.position - lastPos) * 50.2008f;
            var posCalc = (player.transform.position - lastPos);

            if (posCalc.x < -0.001f || posCalc.x > 0.001f || posCalc.y < -0.001f || posCalc.y > 0.001f)
            {
                lastMovement = posCalc;
            }

            lastPos = player.transform.position;

            var dfs = player.transform.localPosition;
            dfs.z = 0f;
            player.transform.localPosition = dfs;
        }

        void UpdateSpeeds()
        {
            var currentModel = PlayerPlugin.CurrentModel(playerIndex);

            var idl = (float)currentModel.values["Base Move Speed"];
            var bst = (float)currentModel.values["Base Boost Speed"];
            var bstcldwn = (float)currentModel.values["Base Boost Cooldown"];
            var bstmin = (float)currentModel.values["Base Min Boost Time"];
            var bstmax = (float)currentModel.values["Base Max Boost Time"];

            float pitch = PlayerExtensions.Pitch;

            idleSpeed = idl;
            boostSpeed = bst;

            boostCooldown = bstcldwn / pitch;
            minBoostTime = bstmin / pitch;
            maxBoostTime = bstmax / pitch;

            var anim = (Animator)playerObjects["Base"].values["Animator"];

            float x = PlayerExtensions.Pitch;
            if (GameManager.inst.gameState == GameManager.State.Paused)
                x = 0f;

            anim.speed = x;
        }

        void UpdateTailDistance()
        {
            path[0].pos = ((Transform)playerObjects["RB Parent"].values["Transform"]).position;
            path[0].rot = ((Transform)playerObjects["RB Parent"].values["Transform"]).rotation;
            for (int i = 1; i < path.Count; i++)
            {
                int num = i - 1;

                if (i == 2 && !path[1].active)
                {
                    num = i - 2;
                }

                if (Vector3.Distance(path[i].pos, path[num].pos) > tailDistance)
                {
                    Vector3 pos = Vector3.Lerp(path[i].pos, path[num].pos, Time.deltaTime * 12f);
                    Quaternion rot = Quaternion.Lerp(path[i].rot, path[num].rot, Time.deltaTime * 12f);

                    if (tailMode == 0)
                    {
                        path[i].pos = pos;
                        path[i].rot = rot;
                    }

                    if (tailMode > 1)
                    {
                        path[i].pos = new Vector3(RTMath.RoundToNearestDecimal(pos.x, 1), RTMath.RoundToNearestDecimal(pos.y, 1), RTMath.RoundToNearestDecimal(pos.z, 1));

                        var r = rot.eulerAngles;

                        path[i].rot = Quaternion.Euler((int)r.x, (int)r.y, (int)r.z);
                    }
                }
            }
        }

        void UpdateTailTransform()
        {
            if (tailMode == 1)
                return;
            if (GameManager.inst == null || GameManager.inst.gameState != GameManager.State.Paused)
            {
                float num = Time.deltaTime * 200f;
                for (int i = 1; i < path.Count; i++)
                {
                    if (InputDataManager.inst.players.Count > 0 && path.Count >= i && path[i].transform != null && path[i].transform.gameObject.activeSelf)
                    {
                        num *= Vector3.Distance(path[i].lastPos, path[i].pos);
                        path[i].transform.position = Vector3.MoveTowards(path[i].lastPos, path[i].pos, num);
                        path[i].lastPos = path[i].transform.position;
                        path[i].transform.rotation = path[i].rot;
                    }
                }
            }
        }

        void UpdateTailDev()
        {
            if (tailMode != 1)
                return;
            if (GameManager.inst == null || GameManager.inst.gameState != GameManager.State.Paused)
            {
                for (int i = 1; i < path.Count; i++)
                {
                    int num = i;
                    if (boostTail && path[1].active)
                    {
                        num += 1;
                    }

                    if (i == 1)
                    {
                        var delayTracker = (DelayTracker)playerObjects["Boost Tail Base"].values["DelayTracker"];
                        //if (rotateMode != RotateMode.FlipX || rotateMode == RotateMode.FlipX && lastMovement.x > 0f)
                            delayTracker.offset = -i * tailDistance / 2f;
                        //else if (rotateMode == RotateMode.FlipX && lastMovement.x < 0)
                        //    delayTracker.offset = -(-i * tailDistance / 2f);
                        delayTracker.positionOffset = 0.1f * (-i + 5);
                        delayTracker.rotationOffset = 0.1f * (-i + 5);
                    }

                    if (playerObjects.ContainsKey(string.Format("Tail {0} Base", i)))
                    {
                        var delayTracker = (DelayTracker)playerObjects[string.Format("Tail {0} Base", i)].values["DelayTracker"];
                        //if (rotateMode != RotateMode.FlipX || rotateMode == RotateMode.FlipX && lastMovement.x > 0f)
                            delayTracker.offset = -num * tailDistance / 2f;
                        //else if (rotateMode == RotateMode.FlipX && lastMovement.x < 0)
                        //    delayTracker.offset = -(-num * tailDistance / 2f);
                        delayTracker.positionOffset = 0.1f * (-num + 5);
                        delayTracker.rotationOffset = 0.1f * (-num + 5);
                    }
                }
            }
        }

        void UpdateTailSizes()
        {
            var currentModel = PlayerPlugin.CurrentModel(playerIndex);
            if (currentModel == null)
                return;
            for (int i = 1; i < 4; i++)
            {
                var t2 = (Vector2)currentModel.values[string.Format("Tail {0} Scale", i)];

                playerObjects[string.Format("Tail {0}", i)].gameObject.transform.localScale = new Vector3(t2.x, t2.y, 1f);
            }
        }

        void UpdateTrailLengths()
        {
            if (PlayerPlugin.CurrentModel(playerIndex) == null)
                return;

            var currentModel = PlayerPlugin.CurrentModel(playerIndex);

            var headTrail = (TrailRenderer)playerObjects["Head Trail"].values["TrailRenderer"];
            var boostTrail = (TrailRenderer)playerObjects["Boost Trail"].values["TrailRenderer"];

            headTrail.time = (float)currentModel.values["Head Trail Time"] / PlayerExtensions.Pitch;
            boostTrail.time = (float)currentModel.values["Boost Trail Time"] / PlayerExtensions.Pitch;

            for (int i = 1; i < 4; i++)
            {
                var tailTrail = (TrailRenderer)playerObjects[string.Format("Tail {0}", i)].values["TrailRenderer"];

                tailTrail.time = (float)currentModel.values[string.Format("Tail {0} Trail Time", i)] / PlayerExtensions.Pitch;
            }
        }

        #endregion

        #region Collision Handlers

        public void OnChildTriggerEnter(Collider2D _other)
        {
            if ((EditorManager.inst != null && !PlayerPlugin.ZenModeInEditor.Value || EditorManager.inst == null) && _other.tag != "Helper" && _other.tag != "Player" && CanTakeDamage && !isBoosting && _other.name != "bullet (Player " + (playerIndex + 1).ToString() + ")")
            {
                PlayerHit();
            }
        }

        public void OnChildTriggerEnterMesh(Collider _other)
        {
            if ((EditorManager.inst != null && !PlayerPlugin.ZenModeInEditor.Value || EditorManager.inst == null) && _other.tag != "Helper" && _other.tag != "Player" && CanTakeDamage && !isBoosting && _other.name != "bullet (Player " + (playerIndex + 1).ToString() + ")")
            {
                PlayerHit();
            }
        }

        public void OnChildTriggerStay(Collider2D _other)
        {
            if ((EditorManager.inst != null && !PlayerPlugin.ZenModeInEditor.Value || EditorManager.inst == null) && _other.tag != "Helper" && _other.tag != "Player" && CanTakeDamage && _other.name != "bullet (Player " + (playerIndex + 1).ToString() + ")")
            {
                PlayerHit();
            }
        }

        public void OnChildTriggerStayMesh(Collider _other)
        {
            if ((EditorManager.inst != null && !PlayerPlugin.ZenModeInEditor.Value || EditorManager.inst == null) && _other.tag != "Helper" && _other.tag != "Player" && CanTakeDamage && _other.name != "bullet (Player " + (playerIndex + 1).ToString() + ")")
            {
                PlayerHit();
            }
        }

        #endregion

        #region Init

        void PlayerHit()
        {
            var rb = (Rigidbody2D)playerObjects["RB Parent"].values["Rigidbody2D"];
            var anim = (Animator)playerObjects["Base"].values["Animator"];
            var player = playerObjects["RB Parent"].gameObject;

            if (CanTakeDamage && PlayerAlive)
            {
                InitBeforeHit();
                InputDataManager.inst.players[playerIndex].health--;
                if (PlayerAlive)
                {
                    anim.SetTrigger("hurt");
                }
                if (playerHitEvent != null)
                {
                    playerHitEvent(InputDataManager.inst.players[playerIndex].health, rb.position);
                }

                EvaluateHitCode();
            }
        }

        IEnumerator BoostCooldownLoop()
        {
            var player = playerObjects["RB Parent"].gameObject;
            var currentModel = PlayerPlugin.CurrentModel(playerIndex);
            var headTrail = (TrailRenderer)playerObjects["Boost Trail"].values["TrailRenderer"];
            if ((bool)currentModel.values["Head Trail Emitting"])
            {
                headTrail.emitting = false;
            }

            DOTween.To(delegate (float x)
            {
                stretchVector = new Vector2(x, -x);
            }, stretchAmount * 1.5f, 0f, 1.5f).SetEase(DataManager.inst.AnimationList[stretchEasing].Animation);

            yield return new WaitForSeconds(boostCooldown / PlayerExtensions.Pitch);
            CanBoost = true;
            if (PlayerPlugin.PlaySoundR.Value)
            {
                AudioManager.inst.PlaySound("boost_recover");
            }

            if (boostTail)
            {
                path[1].active = true;
                var tweener = playerObjects["Boost Tail Base"].gameObject.transform.DOScale(Vector3.one, 0.1f / PlayerExtensions.Pitch).SetEase(DataManager.inst.AnimationList[9].Animation);
                tweener.OnComplete(delegate ()
                {
                    animatingBoost = false;
                });
            }
            yield break;
        }

        IEnumerator Kill()
        {
            Debug.LogFormat("{0}Player {1} died at {2} Controller: {3}", PlayerPlugin.className, playerIndex, AudioManager.inst.CurrentAudioSource.time, Actions.Device);

            isDead = true;
            if (playerDeathEvent != null)
            {
                playerDeathEvent(((Rigidbody2D)playerObjects["RB Parent"].values["Rigidbody2D"]).position);
            }
            InputDataManager.inst.players[playerIndex].active = false;
            InputDataManager.inst.players[playerIndex].health = 0;
            ((Animator)playerObjects["Base"].values["Animator"]).SetTrigger("kill");
            InputDataManager.inst.SetControllerRumble(playerIndex, 1f);
            EvaluateDeathCode();
            yield return new WaitForSecondsRealtime(0.2f);
            PlayerPlugin.players.Remove(this);
            Destroy(health);
            Destroy(gameObject);
            InputDataManager.inst.StopControllerRumble(playerIndex);
            yield break;
        }

        public void InitMidSpawn()
        {
            Debug.LogFormat("{0}InitMidSpawn: {1}", PlayerPlugin.className, playerIndex);
            CanMove = true;
            CanBoost = true;
        }

        public void InitAfterSpawn()
        {
            Debug.LogFormat("{0}InitAfterSpawn: {1}", PlayerPlugin.className, playerIndex);
            if (boostCoroutine != null)
            {
                StopCoroutine(boostCoroutine);
            }
            CanMove = true;
            CanBoost = true;
            CanTakeDamage = true;
        }

        void StartBoost()
        {
            if (CanBoost && !isBoosting)
            {
                var anim = (Animator)playerObjects["Base"].values["Animator"];

                startBoostTime = Time.time;
                InitBeforeBoost();
                anim.SetTrigger("boost");

                var ps = (ParticleSystem)playerObjects["Boost Particles"].values["ParticleSystem"];
                var emission = ps.emission;

                var currentModel = PlayerPlugin.CurrentModel(playerIndex);
                var headTrail = (TrailRenderer)playerObjects["Boost Trail"].values["TrailRenderer"];

                if (emission.enabled)
                {
                    ps.Play();
                }
                if ((bool)currentModel.values["Head Trail Emitting"])
                {
                    headTrail.emitting = true;
                }

                if (PlayerPlugin.PlaySoundB.Value)
                {
                    AudioManager.inst.PlaySound("boost");
                }

                CreatePulse();

                stretchVector = new Vector2(stretchAmount * 1.5f, -(stretchAmount * 1.5f));

                if (boostTail)
                {
                    path[1].active = false;
                    animatingBoost = true;
                    playerObjects["Boost Tail Base"].gameObject.transform.DOScale(Vector3.zero, 0.05f / PlayerExtensions.Pitch).SetEase(DataManager.inst.AnimationList[2].Animation);
                }

                EvaluateBoostCode();
            }
        }

        public void InitBeforeBoost()
        {
            CanBoost = false;
            isBoosting = true;
            CanTakeDamage = false;
        }

        public void InitMidBoost(bool _forceToNormal = false)
        {
            if (_forceToNormal)
            {
                float num = Time.time - startBoostTime;
                StartCoroutine(BoostCancel((num < minBoostTime) ? (minBoostTime - num) : 0f));
                return;
            }
            isBoosting = false;
            CanTakeDamage = true;
        }

        public IEnumerator BoostCancel(float _offset)
        {
            var anim = (Animator)playerObjects["Base"].values["Animator"];

            Debug.LogFormat("{0}Player Boost Cancel: {1} with offset: {2}", PlayerPlugin.className, playerIndex, _offset);
            isBoostCancelled = true;
            yield return new WaitForSeconds(_offset);
            isBoosting = false;
            if (!isTakingHit)
            {
                CanTakeDamage = true;
                anim.SetTrigger("boost_cancel");
                yield return new WaitForSeconds(0.1f);
                InitAfterBoost();
            }
            else
            {
                float num = (Time.time - startHurtTime) / 2.5f;
                if (num < 1f)
                {
                    anim.Play("Hurt", -1, num);
                }
                else
                {
                    anim.SetTrigger("boost_cancel");
                    InitAfterHit();
                }
                yield return new WaitForSeconds(0.1f);
                InitAfterBoost();
            }
            anim.SetTrigger("boost_cancel");
            yield break;
        }

        //Look into making custom damage offsets
        public IEnumerator DamageSetDelay(float _offset)
        {
            yield return new WaitForSeconds(_offset);
            Debug.LogFormat("{0}Player can now be damaged.", PlayerPlugin.className);
            CanTakeDamage = true;
            yield break;
        }

        public void InitAfterBoost()
        {
            isBoosting = false;
            isBoostCancelled = false;
            boostCoroutine = StartCoroutine(BoostCooldownLoop());
        }

        public void InitBeforeHit()
        {
            startHurtTime = Time.time;
            CanBoost = true;
            isBoosting = false;
            isTakingHit = true;
            CanTakeDamage = false;
            if (DataManager.inst.GetCurrentLanguage() == "pirate")
            {
                AudioManager.inst.PlaySound("pirate_KillPlayer");
            }
            else
            {
                AudioManager.inst.PlaySound("HurtPlayer");
            }
        }

        public void InitAfterHit()
        {
            Debug.LogFormat("{0}InitAfterHit: {1}", PlayerPlugin.className, playerIndex);
            isTakingHit = false;
            CanTakeDamage = true;
        }

        public void ResetMovement()
        {
            Debug.LogFormat("{0}ResetMovement: {1}", PlayerPlugin.className, playerIndex);
            if (boostCoroutine != null)
            {
                StopCoroutine(boostCoroutine);
            }
            isBoosting = false;
            CanMove = true;
            CanBoost = true;
        }

        public void PlaySpawnParticles()
        {
            if (playerObjects["Head"].gameObject.transform.Find("spawn-implosion").GetComponent<ParticleSystem>())
            {
                playerObjects["Head"].gameObject.transform.Find("spawn-implosion").GetComponent<ParticleSystem>().Play();
            }
        }
        public void PlayDeathParticles()
        {
            Debug.LogFormat("{0}PlayDeathParticles: {1}", PlayerPlugin.className, playerIndex);
            if (playerObjects["Head"].gameObject.transform.Find("death-explosion").GetComponent<Animator>())
            {
                playerObjects["Head"].gameObject.transform.Find("death-explosion").GetComponent<ParticleSystem>().Play();
            }
        }

        public void PlayHitParticles()
        {
            Debug.LogFormat("{0}PlayHitParticles: {1}", PlayerPlugin.className, playerIndex);
            if (playerObjects["Head"].gameObject.transform.Find("burst-explosion").GetComponent<Animator>())
            {
                playerObjects["Head"].gameObject.transform.Find("burst-explosion").GetComponent<ParticleSystem>().Play();
            }
        }

        public void SetColor(Color _col, Color _colTail)
        {
            var currentModel = PlayerPlugin.CurrentModel(playerIndex);
            if (currentModel != null)
            {
                if (playerObjects.ContainsKey("Head") && playerObjects["Head"].values["MeshRenderer"] != null)
                {
                    int colStart = (int)currentModel.values["Head Color"];
                    var colStartHex = (string)currentModel.values["Head Custom Color"];
                    float alphaStart = (float)currentModel.values["Head Opacity"];

                    ((MeshRenderer)playerObjects["Head"].values["MeshRenderer"]).material.color = GetColor(colStart, alphaStart, colStartHex);
                }
                //new Color(_col.r, _col.g, _col.b, 1f);
                if (playerObjects.ContainsKey("Head") && playerObjects["Head"].gameObject.transform.Find("burst-explosion").GetComponent<ParticleSystem>() != null)
                {
                    int colStart = (int)currentModel.values["Head Color"];
                    var colStartHex = (string)currentModel.values["Head Custom Color"];
                    float alphaStart = (float)currentModel.values["Head Opacity"];

                    var main = playerObjects["Head"].gameObject.transform.Find("burst-explosion").GetComponent<ParticleSystem>().main;
                    main.startColor = new ParticleSystem.MinMaxGradient(GetColor(colStart, alphaStart, colStartHex));
                }
                if (playerObjects.ContainsKey("Head") && playerObjects["Head"].gameObject.transform.Find("death-explosion").GetComponent<ParticleSystem>() != null)
                {
                    var main = playerObjects["Head"].gameObject.transform.Find("death-explosion").GetComponent<ParticleSystem>().main;
                    main.startColor = new ParticleSystem.MinMaxGradient(_col);
                }
                if (playerObjects.ContainsKey("Head") && playerObjects["Head"].gameObject.transform.Find("spawn-implosion").GetComponent<ParticleSystem>() != null)
                {
                    int colStart = (int)currentModel.values["Head Color"];
                    var colStartHex = (string)currentModel.values["Head Custom Color"];
                    float alphaStart = (float)currentModel.values["Head Opacity"];

                    var main = playerObjects["Head"].gameObject.transform.Find("spawn-implosion").GetComponent<ParticleSystem>().main;
                    main.startColor = new ParticleSystem.MinMaxGradient(GetColor(colStart, alphaStart, colStartHex));
                }
                if (playerObjects.ContainsKey("Boost") && playerObjects["Boost"].values["MeshRenderer"] != null)
                {
                    int colStart = (int)currentModel.values["Boost Color"];
                    var colStartHex = (string)currentModel.values["Boost Custom Color"];
                    float alphaStart = (float)currentModel.values["Boost Opacity"];

                    ((MeshRenderer)playerObjects["Boost"].values["MeshRenderer"]).material.color = GetColor(colStart, alphaStart, colStartHex);
                }

                if (playerObjects.ContainsKey("Boost Tail") && playerObjects["Boost Tail"].values["MeshRenderer"] != null)
                {
                    int colStart = (int)currentModel.values["Tail Boost Color"];
                    var colStartHex = (string)currentModel.values["Tail Boost Custom Color"];
                    float alphaStart = (float)currentModel.values["Tail Boost Opacity"];

                    ((MeshRenderer)playerObjects["Boost Tail"].values["MeshRenderer"]).material.color = GetColor(colStart, alphaStart, colStartHex);
                }

                //GUI Bar
                {
                    int baseCol = (int)currentModel.values["GUI Health Base Color"];
                    int topCol = (int)currentModel.values["GUI Health Top Color"];
                    string baseColHex = (string)currentModel.values["GUI Health Base Custom Color"];
                    string topColHex = (string)currentModel.values["GUI Health Top Custom Color"];
                    float baseAlpha = (float)currentModel.values["GUI Health Base Opacity"];
                    float topAlpha = (float)currentModel.values["GUI Health Top Opacity"];

                    for (int i = 0; i < healthObjects.Count; i++)
                    {
                        if (healthObjects[i].image != null)
                        {
                            healthObjects[i].image.color = GetColor(topCol, topAlpha, topColHex);
                        }
                    }

                    barBaseIm.color = GetColor(baseCol, baseAlpha, baseColHex);
                    barIm.color = GetColor(topCol, topAlpha, topColHex);
                }

                for (int i = 1; i < 4; i++)
                {
                    int colStart = (int)currentModel.values[string.Format("Tail {0} Trail Start Color", i)];
                    var colStartHex = (string)currentModel.values[string.Format("Tail {0} Trail Start Custom Color", i)];
                    float alphaStart = (float)currentModel.values[string.Format("Tail {0} Trail Start Opacity", i)];
                    int colEnd = (int)currentModel.values[string.Format("Tail {0} Trail End Color", i)];
                    var colEndHex = (string)currentModel.values[string.Format("Tail {0} Trail End Custom Color", i)];
                    float alphaEnd = (float)currentModel.values[string.Format("Tail {0} Trail End Opacity", i)];

                    var psCol = (int)currentModel.values[string.Format("Tail {0} Particles Color", i)];
                    var psColHex = (string)currentModel.values[string.Format("Tail {0} Particles Custom Color", i)];

                    ((MeshRenderer)playerObjects[string.Format("Tail {0}", i)].values["MeshRenderer"]).material.color = _colTail;

                    var trailRenderer = (TrailRenderer)playerObjects[string.Format("Tail {0}", i)].values["TrailRenderer"];

                    trailRenderer.startColor = GetColor(colStart, alphaStart, colStartHex);
                    trailRenderer.endColor = GetColor(colEnd, alphaEnd, colEndHex);
                }

                if (playerObjects["Head Trail"].values["TrailRenderer"] != null && (bool)currentModel.values["Head Trail Emitting"])
                {
                    int colStart = (int)currentModel.values["Head Trail Start Color"];
                    var colStartHex = (string)currentModel.values["Head Trail Start Custom Color"];
                    float alphaStart = (float)currentModel.values["Head Trail Start Opacity"];
                    int colEnd = (int)currentModel.values["Head Trail End Color"];
                    var colEndHex = (string)currentModel.values["Head Trail End Custom Color"];
                    float alphaEnd = (float)currentModel.values["Head Trail End Opacity"];

                    var trailRenderer = (TrailRenderer)playerObjects["Head Trail"].values["TrailRenderer"];

                    trailRenderer.startColor = GetColor(colStart, alphaStart, colStartHex);
                    trailRenderer.endColor = GetColor(colEnd, alphaEnd, colEndHex);
                }
                if (playerObjects["Head Particles"].values["ParticleSystem"] != null && (bool)currentModel.values["Head Particles Emitting"])
                {
                    var colStart = (int)currentModel.values["Head Particles Color"];
                    var colStartHex = (string)currentModel.values["Head Particles Custom Color"];

                    var ps = (ParticleSystem)playerObjects["Head Particles"].values["ParticleSystem"];
                    var main = ps.main;

                    main.startColor = PlayerHelpers.GetColor(playerIndex, colStart, colStartHex);
                }
                if (playerObjects["Boost Trail"].values["TrailRenderer"] != null && (bool)currentModel.values["Boost Trail Emitting"])
                {
                    var colStart = (int)currentModel.values["Boost Trail Start Color"];
                    var colStartHex = (string)currentModel.values["Boost Trail Start Custom Color"];
                    var alphaStart = (float)currentModel.values["Boost Trail Start Opacity"];
                    var colEnd = (int)currentModel.values["Boost Trail End Color"];
                    var colEndHex = (string)currentModel.values["Boost Trail End Custom Color"];
                    var alphaEnd = (float)currentModel.values["Boost Trail End Opacity"];

                    var trailRenderer = (TrailRenderer)playerObjects["Boost Trail"].values["TrailRenderer"];

                    trailRenderer.startColor = GetColor(colStart, alphaStart, colStartHex);
                    trailRenderer.endColor = GetColor(colEnd, alphaEnd, colEndHex);
                }
                if (playerObjects["Boost Particles"].values["ParticleSystem"] != null && (bool)currentModel.values["Boost Particles Emitting"])
                {
                    var colStart = (int)currentModel.values["Boost Particles Color"];
                    var colHex = (string)currentModel.values["Boost Particles Custom Color"];

                    var ps = (ParticleSystem)playerObjects["Boost Particles"].values["ParticleSystem"];
                    var main = ps.main;

                    main.startColor = PlayerHelpers.GetColor(playerIndex, colStart, colHex);
                }
            }
        }

        #endregion

        #region Update Values

        bool hasUpdated = false;

        public void updatePlayer()
        {
            var currentModel = PlayerPlugin.CurrentModel(playerIndex);
            var rb = (Rigidbody2D)playerObjects["RB Parent"].values["Rigidbody2D"];

            //New NameTag
            {
                Destroy(canvas);
                canvas = new GameObject("Name Tag Canvas" + (playerIndex + 1).ToString());
                canvas.transform.SetParent(transform);
                canvas.transform.localScale = Vector3.one;
                canvas.transform.localRotation = Quaternion.identity;

                var bae = Instantiate(ObjectManager.inst.objectPrefabs[0].options[0]);
                bae.transform.SetParent(canvas.transform);
                bae.transform.localScale = Vector3.one;
                bae.transform.localRotation = Quaternion.identity;

                bae.transform.GetChild(0).transform.localScale = new Vector3(6.5f, 1.5f, 1f);
                bae.transform.GetChild(0).transform.localPosition = new Vector3(0f, 2.5f, -0.3f);

                healthBase = bae.GetComponentInChildren<MeshRenderer>();
                healthBase.enabled = true;

                Destroy(bae.GetComponentInChildren<RTFunctions.Functions.Components.RTObject>());
                Destroy(bae.GetComponentInChildren<SelectObjectInEditor>());
                Destroy(bae.GetComponentInChildren<Collider2D>());

                var tae = Instantiate(ObjectManager.inst.objectPrefabs[4].options[0]);
                tae.transform.SetParent(canvas.transform);
                tae.transform.localScale = Vector3.one;
                tae.transform.localRotation = Quaternion.identity;

                tae.transform.GetChild(0).transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                tae.transform.GetChild(0).transform.localPosition = new Vector3(0f, 2.5f, -0.3f);

                textMesh = tae.GetComponentInChildren<TextMeshPro>();

                var d = canvas.AddComponent<DelayTracker>();
                d.leader = playerObjects["RB Parent"].gameObject.transform;
                d.scaleParent = false;
                d.rotationParent = false;
                d.player = this;
                d.positionOffset = 0.9f;
            }

            //Set new transform values
            if (currentModel != null)
            {
                //Head Shape
                {
                    int s = ((Vector2Int)currentModel.values["Head Shape"]).x;
                    int so = ((Vector2Int)currentModel.values["Head Shape"]).y;

                    s = Mathf.Clamp(s, 0, ObjectManager.inst.objectPrefabs.Count - 1);
                    so = Mathf.Clamp(so, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);

                    if (s != 4 && s != 6)
                        ((MeshFilter)playerObjects["Head"].values["MeshFilter"]).mesh = ObjectManager.inst.objectPrefabs[s].options[so].GetComponentInChildren<MeshFilter>().mesh;
                    else
                        ((MeshFilter)playerObjects["Head"].values["MeshFilter"]).mesh = ObjectManager.inst.objectPrefabs[0].options[0].GetComponentInChildren<MeshFilter>().mesh;
                }

                //Boost Shape
                {
                    int s = ((Vector2Int)currentModel.values["Boost Shape"]).x;
                    int so = ((Vector2Int)currentModel.values["Boost Shape"]).y;

                    s = Mathf.Clamp(s, 0, ObjectManager.inst.objectPrefabs.Count - 1);
                    so = Mathf.Clamp(so, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);

                    if (s != 4 && s != 6)
                        ((MeshFilter)playerObjects["Boost"].values["MeshFilter"]).mesh = ObjectManager.inst.objectPrefabs[s].options[so].GetComponentInChildren<MeshFilter>().mesh;
                    else
                        ((MeshFilter)playerObjects["Boost"].values["MeshFilter"]).mesh = ObjectManager.inst.objectPrefabs[0].options[0].GetComponentInChildren<MeshFilter>().mesh;
                }

                //Tail Boost Shape
                {
                    int s = ((Vector2Int)currentModel.values["Tail Boost Shape"]).x;
                    int so = ((Vector2Int)currentModel.values["Tail Boost Shape"]).y;

                    s = Mathf.Clamp(s, 0, ObjectManager.inst.objectPrefabs.Count - 1);
                    so = Mathf.Clamp(so, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);

                    if (s != 4 && s != 6)
                        ((MeshFilter)playerObjects["Boost Tail"].values["MeshFilter"]).mesh = ObjectManager.inst.objectPrefabs[s].options[so].GetComponentInChildren<MeshFilter>().mesh;
                    else
                        ((MeshFilter)playerObjects["Boost Tail"].values["MeshFilter"]).mesh = ObjectManager.inst.objectPrefabs[0].options[0].GetComponentInChildren<MeshFilter>().mesh;
                }

                //Tail 1 Shape
                for (int i = 1; i < 4; i++)
                {
                    int s = ((Vector2Int)currentModel.values[string.Format("Tail {0} Shape", i)]).x;
                    int so = ((Vector2Int)currentModel.values[string.Format("Tail {0} Shape", i)]).y;

                    s = Mathf.Clamp(s, 0, ObjectManager.inst.objectPrefabs.Count - 1);
                    so = Mathf.Clamp(so, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);

                    if (s != 4 && s != 6)
                        ((MeshFilter)playerObjects[string.Format("Tail {0}", i)].values["MeshFilter"]).mesh = ObjectManager.inst.objectPrefabs[s].options[so].GetComponentInChildren<MeshFilter>().mesh;
                    else
                        ((MeshFilter)playerObjects[string.Format("Tail {0}", i)].values["MeshFilter"]).mesh = ObjectManager.inst.objectPrefabs[0].options[0].GetComponentInChildren<MeshFilter>().mesh;
                }

                var h1 = (Vector2)currentModel.values["Head Position"];
                var h2 = (Vector2)currentModel.values["Head Scale"];
                var h3 = (float)currentModel.values["Head Rotation"];

                if (PlayerPlugin.debug)
                    Debug.LogFormat("{0}Rendering Head\nPos: {1}\nSca: {2}\nRot: {3}", PlayerPlugin.className, h1, h2, h3);

                playerObjects["Head"].gameObject.transform.localPosition = new Vector3(h1.x, h1.y, 0f);
                playerObjects["Head"].gameObject.transform.localScale = new Vector3(h2.x, h2.y, 1f);
                playerObjects["Head"].gameObject.transform.localEulerAngles = new Vector3(0f, 0f, h3);

                var b1 = (Vector2)currentModel.values["Boost Position"];
                var b2 = (Vector2)currentModel.values["Boost Scale"];
                var b3 = (float)currentModel.values["Boost Rotation"];

                if (PlayerPlugin.debug)
                    Debug.LogFormat("{0}Rendering Boost\nPos: {1}\nSca: {2}\nRot: {3}", PlayerPlugin.className, b1, b2, b3);

                ((MeshRenderer)playerObjects["Boost"].values["MeshRenderer"]).enabled = (bool)currentModel.values["Boost Active"];
                playerObjects["Boost Base"].gameObject.transform.localPosition = new Vector3(b1.x, b1.y, 0.1f);
                playerObjects["Boost Base"].gameObject.transform.localScale = new Vector3(b2.x, b2.y, 1f);
                playerObjects["Boost Base"].gameObject.transform.localEulerAngles = new Vector3(0f, 0f, b3);

                tailDistance = (float)currentModel.values["Tail Base Distance"];
                tailMode = (int)currentModel.values["Tail Base Mode"];

                tailGrows = (bool)currentModel.values["Tail Base Grows"];

                boostTail = (bool)currentModel.values["Tail Boost Active"];

                playerObjects["Boost Tail Base"].gameObject.SetActive(boostTail);

                var fp = (Vector2)currentModel.values["Face Position"];
                playerObjects["Face Parent"].gameObject.transform.localPosition = new Vector3(fp.x, fp.y, 0f);

                if (!isBoosting)
                {
                    path[1].active = boostTail;
                }

                //Stretch
                {
                    stretch = (bool)currentModel.values["Stretch Active"];
                    stretchAmount = (float)currentModel.values["Stretch Amount"];
                    stretchEasing = (int)currentModel.values["Stretch Easing"];
                }

                var bt1 = (Vector2)currentModel.values["Tail Boost Position"];
                var bt2 = (Vector2)currentModel.values["Tail Boost Scale"];
                var bt3 = (float)currentModel.values["Tail Boost Rotation"];

                playerObjects["Boost Tail"].gameObject.SetActive(boostTail);
                if (boostTail)
                {
                    playerObjects["Boost Tail"].gameObject.transform.localPosition = new Vector3(bt1.x, bt1.y, 0.1f);
                    playerObjects["Boost Tail"].gameObject.transform.localScale = new Vector3(bt2.x, bt2.y, 1f);
                    playerObjects["Boost Tail"].gameObject.transform.localEulerAngles = new Vector3(0f, 0f, bt3);
                }

                rotateMode = (RotateMode)(int)currentModel.values["Base Rotate Mode"];

                updateMode = PlayerPlugin.TailUpdateMode.Value;

                ((CircleCollider2D)playerObjects["RB Parent"].values["CircleCollider2D"]).isTrigger = EditorManager.inst != null && PlayerPlugin.ZenEditorIncludesSolid.Value && PlayerPlugin.ZenModeInEditor.Value;
                ((PolygonCollider2D)playerObjects["RB Parent"].values["PolygonCollider2D"]).isTrigger = EditorManager.inst != null && PlayerPlugin.ZenEditorIncludesSolid.Value && PlayerPlugin.ZenModeInEditor.Value;

                var colAcc = (bool)currentModel.values["Base Collision Accurate"];
                if (colAcc)
                {
                    ((CircleCollider2D)playerObjects["RB Parent"].values["CircleCollider2D"]).enabled = false;
                    ((PolygonCollider2D)playerObjects["RB Parent"].values["PolygonCollider2D"]).enabled = true;
                    ((PolygonCollider2D)playerObjects["RB Parent"].values["PolygonCollider2D"]).CreateCollider((MeshFilter)playerObjects["Head"].values["MeshFilter"]);
                }
                else
                {
                    ((PolygonCollider2D)playerObjects["RB Parent"].values["PolygonCollider2D"]).enabled = false;
                    ((CircleCollider2D)playerObjects["RB Parent"].values["CircleCollider2D"]).enabled = true;
                }

                for (int i = 1; i < 4; i++)
                {
                    //int num = i;
                    //if (boostTail)
                    //{
                    //    num += 1;
                    //}

                    //var delayTracker = (DelayTracker)playerObjects[string.Format("Tail {0} Base", i)].values["DelayTracker"];
                    //delayTracker.offset = -num * tailDistance / 2f;
                    //delayTracker.followSharpness = 0.1f * (-num + 4);

                    var t1 = (Vector2)currentModel.values[string.Format("Tail {0} Position", i)];
                    var t2 = (Vector2)currentModel.values[string.Format("Tail {0} Scale", i)];
                    var t3 = (float)currentModel.values[string.Format("Tail {0} Rotation", i)];

                    Debug.LogFormat("{0}Rendering Tail {1}\nPos: {2}\nSca: {3}\nRot: {4}", PlayerPlugin.className, i, t1, t2, t3);

                    ((MeshRenderer)playerObjects[string.Format("Tail {0}", i)].values["MeshRenderer"]).enabled = (bool)currentModel.values[string.Format("Tail {0} Active", i)];
                    playerObjects[string.Format("Tail {0}", i)].gameObject.transform.localPosition = new Vector3(t1.x, t1.y, 0.1f);
                    playerObjects[string.Format("Tail {0}", i)].gameObject.transform.localScale = new Vector3(t2.x, t2.y, 1f);
                    playerObjects[string.Format("Tail {0}", i)].gameObject.transform.localEulerAngles = new Vector3(0f, 0f, t3);
                }

                //Health
                {
                    if (EditorManager.inst == null && DataManager.inst.GetSettingEnum("ArcadeDifficulty", 0) == 3)
                    {
                        CustomPlayer.health = 1;
                    }
                    else if (EditorManager.inst != null)
                    {
                        CustomPlayer.health = (int)currentModel.values["Base Health"];
                    }

                    UpdateTail(CustomPlayer.health, rb.position);
                    initialHealthCount = CustomPlayer.health;
                }

                //Health Images
                {
                    if (RTFile.FileExists(RTFile.ApplicationDirectory + RTFile.basePath + "health.png") && !PlayerPlugin.AssetsGlobal.Value)
                    {
                        if (PlayerPlugin.debug)
                            Debug.LogFormat("{0}Updating Health GUI", PlayerPlugin.className);

                        foreach (var health in healthObjects)
                        {
                            if (SpriteManager.inst != null && health.image != null)
                            {
                                SpriteManager.GetSprite(RTFile.ApplicationDirectory + RTFile.basePath + "health.png", health.image);
                            }
                        }
                    }
                    else if (RTFile.FileExists(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/health.png") && PlayerPlugin.AssetsGlobal.Value)
                    {
                        if (PlayerPlugin.debug)
                            Debug.LogFormat("{0}Updating Health GUI", PlayerPlugin.className);

                        foreach (var health in healthObjects)
                        {
                            if (SpriteManager.inst != null && health.image != null)
                            {
                                SpriteManager.GetSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/health.png", health.image);
                            }
                        }
                    }
                    else
                    {
                        if (PlayerPlugin.debug)
                            Debug.LogFormat("{0}Updating Health GUI without image", PlayerPlugin.className);

                        foreach (var health in healthObjects)
                        {
                            if (health.image != null && PlayerPlugin.healthSprite != null)
                                health.image.sprite = PlayerPlugin.healthSprite;
                        }
                    }
                }

                //Trail
                {
                    var headTrail = (TrailRenderer)playerObjects["Head Trail"].values["TrailRenderer"];

                    playerObjects["Head Trail"].gameObject.transform.localPosition = (Vector2)currentModel.values["Head Trail Position Offset"];

                    headTrail.enabled = (bool)currentModel.values["Head Trail Emitting"];
                    //headTrail.time = (float)currentModel.values["Head Trail Time"];
                    headTrail.startWidth = (float)currentModel.values["Head Trail Start Width"];
                    headTrail.endWidth = (float)currentModel.values["Head Trail End Width"];
                }

                //Particles
                {
                    var headParticles = (ParticleSystem)playerObjects["Head Particles"].values["ParticleSystem"];
                    var headParticlesRenderer = (ParticleSystemRenderer)playerObjects["Head Particles"].values["ParticleSystemRenderer"];

                    var s = ((Vector2Int)currentModel.values["Head Particles Shape"]).x;
                    var so = ((Vector2Int)currentModel.values["Head Particles Shape"]).y;

                    s = Mathf.Clamp(s, 0, ObjectManager.inst.objectPrefabs.Count - 1);
                    so = Mathf.Clamp(so, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);

                    if (s != 4 && s != 6)
                    {
                        headParticlesRenderer.mesh = ObjectManager.inst.objectPrefabs[s].options[so].GetComponentInChildren<MeshFilter>().mesh;
                    }
                    var main = headParticles.main;
                    var emission = headParticles.emission;

                    main.startLifetime = (float)currentModel.values["Head Particles Lifetime"];
                    main.startSpeed = (float)currentModel.values["Head Particles Speed"];

                    emission.enabled = (bool)currentModel.values["Head Particles Emitting"];
                    headParticles.emissionRate = (float)currentModel.values["Head Particles Amount"];

                    var rotationOverLifetime = headParticles.rotationOverLifetime;
                    rotationOverLifetime.enabled = true;
                    rotationOverLifetime.separateAxes = true;
                    rotationOverLifetime.xMultiplier = 0f;
                    rotationOverLifetime.yMultiplier = 0f;
                    rotationOverLifetime.zMultiplier = (float)currentModel.values["Head Particles Rotation"];

                    var forceOverLifetime = headParticles.forceOverLifetime;
                    forceOverLifetime.enabled = true;
                    forceOverLifetime.space = ParticleSystemSimulationSpace.World;
                    forceOverLifetime.xMultiplier = ((Vector2)currentModel.values["Head Particles Force"]).x;
                    forceOverLifetime.yMultiplier = ((Vector2)currentModel.values["Head Particles Force"]).y;

                    var particlesTrail = headParticles.trails;
                    particlesTrail.enabled = (bool)currentModel.values["Head Particles Trail Emitting"];

                    var colorOverLifetime = headParticles.colorOverLifetime;
                    colorOverLifetime.enabled = true;
                    var psCol = colorOverLifetime.color;

                    float alphaStart = (float)currentModel.values["Head Particles Start Opacity"];
                    float alphaEnd = (float)currentModel.values["Head Particles End Opacity"];

                    var gradient = new Gradient();
                    gradient.alphaKeys = new GradientAlphaKey[2]
                    {
                        new GradientAlphaKey(alphaStart, 0f),
                        new GradientAlphaKey(alphaEnd, 1f)
                    };
                    gradient.colorKeys = new GradientColorKey[2]
                    {
                        new GradientColorKey(Color.white, 0f),
                        new GradientColorKey(Color.white, 1f)
                    };

                    psCol.gradient = gradient;

                    colorOverLifetime.color = psCol;

                    var sizeOverLifetime = headParticles.sizeOverLifetime;
                    sizeOverLifetime.enabled = true;

                    var ssss = sizeOverLifetime.size;

                    var sizeStart = (float)currentModel.values["Head Particles Start Scale"];
                    var sizeEnd = (float)currentModel.values["Head Particles End Scale"];

                    var curve = new AnimationCurve(new Keyframe[2]
                    {
                        new Keyframe(0f, sizeStart),
                        new Keyframe(1f, sizeEnd)
                    });

                    ssss.curve = curve;

                    sizeOverLifetime.size = ssss;
                }

                //Boost Trail
                {
                    var headTrail = (TrailRenderer)playerObjects["Boost Trail"].values["TrailRenderer"];
                    headTrail.enabled = (bool)currentModel.values["Boost Trail Emitting"];
                    headTrail.emitting = (bool)currentModel.values["Boost Trail Emitting"];
                    //headTrail.time = (float)currentModel.values["Boost Trail Time"];
                }

                //Boost Particles
                {
                    var headParticles = (ParticleSystem)playerObjects["Boost Particles"].values["ParticleSystem"];
                    var headParticlesRenderer = (ParticleSystemRenderer)playerObjects["Boost Particles"].values["ParticleSystemRenderer"];

                    var s = ((Vector2Int)currentModel.values["Boost Particles Shape"]).x;
                    var so = ((Vector2Int)currentModel.values["Boost Particles Shape"]).y;

                    s = Mathf.Clamp(s, 0, ObjectManager.inst.objectPrefabs.Count - 1);
                    so = Mathf.Clamp(so, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);

                    if (s != 4 && s != 6)
                    {
                        headParticlesRenderer.mesh = ObjectManager.inst.objectPrefabs[s].options[so].GetComponentInChildren<MeshFilter>().mesh;
                    }

                    var main = headParticles.main;
                    var emission = headParticles.emission;

                    main.startLifetime = (float)currentModel.values["Boost Particles Lifetime"];
                    main.startSpeed = (float)currentModel.values["Boost Particles Speed"];

                    emission.enabled = (bool)currentModel.values["Boost Particles Emitting"];
                    headParticles.emissionRate = 0f;
                    emission.burstCount = (int)currentModel.values["Boost Particles Amount"];
                    main.duration = (float)currentModel.values["Boost Particles Duration"];

                    var rotationOverLifetime = headParticles.rotationOverLifetime;
                    rotationOverLifetime.enabled = true;
                    rotationOverLifetime.separateAxes = true;
                    rotationOverLifetime.xMultiplier = 0f;
                    rotationOverLifetime.yMultiplier = 0f;
                    rotationOverLifetime.zMultiplier = (float)currentModel.values["Boost Particles Rotation"];

                    var forceOverLifetime = headParticles.forceOverLifetime;
                    forceOverLifetime.enabled = true;
                    forceOverLifetime.space = ParticleSystemSimulationSpace.World;
                    forceOverLifetime.xMultiplier = ((Vector2)currentModel.values["Boost Particles Force"]).x;
                    forceOverLifetime.yMultiplier = ((Vector2)currentModel.values["Boost Particles Force"]).y;

                    var particlesTrail = headParticles.trails;
                    particlesTrail.enabled = (bool)currentModel.values["Boost Particles Trail Emitting"];

                    var colorOverLifetime = headParticles.colorOverLifetime;
                    colorOverLifetime.enabled = true;
                    var psCol = colorOverLifetime.color;

                    float alphaStart = (float)currentModel.values["Boost Particles Start Opacity"];
                    float alphaEnd = (float)currentModel.values["Boost Particles End Opacity"];

                    var gradient = new Gradient();
                    gradient.alphaKeys = new GradientAlphaKey[2]
                    {
                        new GradientAlphaKey(alphaStart, 0f),
                        new GradientAlphaKey(alphaEnd, 1f)
                    };
                    gradient.colorKeys = new GradientColorKey[2]
                    {
                        new GradientColorKey(Color.white, 0f),
                        new GradientColorKey(Color.white, 1f)
                    };

                    psCol.gradient = gradient;

                    colorOverLifetime.color = psCol;

                    var sizeOverLifetime = headParticles.sizeOverLifetime;
                    sizeOverLifetime.enabled = true;

                    var ssss = sizeOverLifetime.size;

                    var sizeStart = (float)currentModel.values["Boost Particles Start Scale"];
                    var sizeEnd = (float)currentModel.values["Boost Particles End Scale"];

                    var curve = new AnimationCurve(new Keyframe[2]
                    {
                        new Keyframe(0f, sizeStart),
                        new Keyframe(1f, sizeEnd)
                    });

                    ssss.curve = curve;

                    sizeOverLifetime.size = ssss;
                }

                //Tails Trail / Particles
                {
                    for (int i = 1; i < 4; i++)
                    {
                        var headTrail = (TrailRenderer)playerObjects[string.Format("Tail {0}", i)].values["TrailRenderer"];
                        headTrail.enabled = (bool)currentModel.values[string.Format("Tail {0} Trail Emitting", i)];
                        headTrail.emitting = (bool)currentModel.values[string.Format("Tail {0} Trail Emitting", i)];
                        //headTrail.time = (float)currentModel.values[string.Format("Tail {0} Trail Time", i)];
                        headTrail.startWidth = (float)currentModel.values[string.Format("Tail {0} Trail Start Width", i)];
                        headTrail.endWidth = (float)currentModel.values[string.Format("Tail {0} Trail End Width", i)];


                        var headParticles = (ParticleSystem)playerObjects[string.Format("Tail {0} Particles", i)].values["ParticleSystem"];
                        var headParticlesRenderer = (ParticleSystemRenderer)playerObjects[string.Format("Tail {0} Particles", i)].values["ParticleSystemRenderer"];

                        var s = ((Vector2Int)currentModel.values[string.Format("Tail {0} Particles Shape", i)]).x;
                        var so = ((Vector2Int)currentModel.values[string.Format("Tail {0} Particles Shape", i)]).y;

                        s = Mathf.Clamp(s, 0, ObjectManager.inst.objectPrefabs.Count - 1);
                        so = Mathf.Clamp(so, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);

                        if (s != 4 && s != 6)
                        {
                            headParticlesRenderer.mesh = ObjectManager.inst.objectPrefabs[s].options[so].GetComponentInChildren<MeshFilter>().mesh;
                        }
                        var main = headParticles.main;
                        var emission = headParticles.emission;

                        main.startLifetime = (float)currentModel.values[string.Format("Tail {0} Particles Lifetime", i)];
                        main.startSpeed = (float)currentModel.values[string.Format("Tail {0} Particles Speed", i)];

                        emission.enabled = (bool)currentModel.values[string.Format("Tail {0} Particles Emitting", i)];
                        headParticles.emissionRate = (float)currentModel.values[string.Format("Tail {0} Particles Amount", i)];

                        var rotationOverLifetime = headParticles.rotationOverLifetime;
                        rotationOverLifetime.enabled = true;
                        rotationOverLifetime.separateAxes = true;
                        rotationOverLifetime.xMultiplier = 0f;
                        rotationOverLifetime.yMultiplier = 0f;
                        rotationOverLifetime.zMultiplier = (float)currentModel.values[string.Format("Tail {0} Particles Rotation", i)];

                        var forceOverLifetime = headParticles.forceOverLifetime;
                        forceOverLifetime.enabled = true;
                        forceOverLifetime.space = ParticleSystemSimulationSpace.World;
                        forceOverLifetime.xMultiplier = ((Vector2)currentModel.values[string.Format("Tail {0} Particles Force", i)]).x;
                        forceOverLifetime.yMultiplier = ((Vector2)currentModel.values[string.Format("Tail {0} Particles Force", i)]).y;

                        var particlesTrail = headParticles.trails;
                        particlesTrail.enabled = (bool)currentModel.values[string.Format("Tail {0} Particles Trail Emitting", i)];

                        var colorOverLifetime = headParticles.colorOverLifetime;
                        colorOverLifetime.enabled = true;
                        var psCol = colorOverLifetime.color;

                        float alphaStart = (float)currentModel.values[string.Format("Tail {0} Particles Start Opacity", i)];
                        float alphaEnd = (float)currentModel.values[string.Format("Tail {0} Particles End Opacity", i)];

                        var gradient = new Gradient();
                        gradient.alphaKeys = new GradientAlphaKey[2]
                        {
                            new GradientAlphaKey(alphaStart, 0f),
                            new GradientAlphaKey(alphaEnd, 1f)
                        };
                        gradient.colorKeys = new GradientColorKey[2]
                        {
                            new GradientColorKey(Color.white, 0f),
                            new GradientColorKey(Color.white, 1f)
                        };

                        psCol.gradient = gradient;

                        colorOverLifetime.color = psCol;

                        var sizeOverLifetime = headParticles.sizeOverLifetime;
                        sizeOverLifetime.enabled = true;

                        var ssss = sizeOverLifetime.size;

                        var sizeStart = (float)currentModel.values[string.Format("Tail {0} Particles Start Scale", i)];
                        var sizeEnd = (float)currentModel.values[string.Format("Tail {0} Particles End Scale", i)];

                        var curve = new AnimationCurve(new Keyframe[2]
                        {
                        new Keyframe(0f, sizeStart),
                        new Keyframe(1f, sizeEnd)
                        });

                        ssss.curve = curve;

                        sizeOverLifetime.size = ssss;
                    }
                }
            }

            hasUpdated = true;
            CreateAll();
        }

        void updateCustomObjects(string id = "")
        {
            if (customObjects.Count > 0)
            {
                foreach (var obj in customObjects)
                {
                    if (id != "" && obj.Key == id || id == "")
                    {
                        var customObj = obj.Value;

                        if (((Vector2Int)customObj.values["Shape"]).x != 4 && ((Vector2Int)customObj.values["Shape"]).x != 6)
                        {
                            var shape = (Vector2Int)customObj.values["Shape"];
                            var pos = (Vector2)customObj.values["Position"];
                            var sca = (Vector2)customObj.values["Scale"];
                            var rot = (float)customObj.values["Rotation"];

                            var depth = (float)customObj.values["Depth"];

                            int s = Mathf.Clamp(shape.x, 0, ObjectManager.inst.objectPrefabs.Count - 1);
                            int so = Mathf.Clamp(shape.y, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);

                            customObj.gameObject = Instantiate(ObjectManager.inst.objectPrefabs[s].options[so]);
                            customObj.gameObject.transform.SetParent(transform);
                            customObj.gameObject.transform.localScale = Vector3.one;
                            customObj.gameObject.transform.localRotation = Quaternion.identity;

                            var delayTracker = customObj.gameObject.AddComponent<DelayTracker>();
                            delayTracker.offset = 0;
                            delayTracker.positionOffset = (float)customObj.values["Parent Position Offset"];
                            delayTracker.scaleOffset = (float)customObj.values["Parent Scale Offset"];
                            delayTracker.rotationOffset = (float)customObj.values["Parent Rotation Offset"];
                            delayTracker.scaleParent = (bool)customObj.values["Parent Scale Active"];
                            delayTracker.rotationParent = (bool)customObj.values["Parent Rotation Active"];
                            delayTracker.player = this;
                            
                            switch ((int)customObj.values["Parent"])
                            {
                                case 0:
                                    {
                                        delayTracker.leader = playerObjects["RB Parent"].gameObject.transform;
                                        break;
                                    }
                                case 1:
                                    {
                                        delayTracker.leader = playerObjects["Boost Base"].gameObject.transform;
                                        break;
                                    }
                                case 2:
                                    {
                                        delayTracker.leader = playerObjects["Boost Tail Base"].gameObject.transform;
                                        break;
                                    }
                                case 3:
                                    {
                                        delayTracker.leader = playerObjects["Tail 1 Base"].gameObject.transform;
                                        break;
                                    }
                                case 4:
                                    {
                                        delayTracker.leader = playerObjects["Tail 2 Base"].gameObject.transform;
                                        break;
                                    }
                                case 5:
                                    {
                                        delayTracker.leader = playerObjects["Tail 3 Base"].gameObject.transform;
                                        break;
                                    }
                                case 6:
                                    {
                                        delayTracker.leader = playerObjects["Face Parent"].gameObject.transform;
                                        break;
                                    }
                            }

                            customObj.gameObject.transform.GetChild(0).localPosition = new Vector3(pos.x, pos.y, depth);
                            customObj.gameObject.transform.GetChild(0).localScale = new Vector3(sca.x, sca.y, 1f);
                            customObj.gameObject.transform.GetChild(0).localEulerAngles = new Vector3(0f, 0f, rot);

                            customObj.gameObject.tag = "Helper";
                            customObj.gameObject.transform.GetChild(0).tag = "Helper";

                            customObj.values["MeshRenderer"] = customObj.gameObject.GetComponentInChildren<MeshRenderer>();
                        }
                    }
                }
            }
        }

        void CreateAll()
        {
            var currentModel = PlayerPlugin.CurrentModel(playerIndex);

            Dictionary<string, object> dictionary = (Dictionary<string, object>)currentModel.values["Custom Objects"];

            foreach (var obj in customObjects)
            {
                if (obj.Value.gameObject != null)
                {
                    if (PlayerPlugin.debug)
                        Debug.LogFormat("{0}Destroying Custom Object! {1}", PlayerPlugin.className, obj.Key);
                    Destroy(obj.Value.gameObject);
                }
            }

            customObjects.Clear();
            if (dictionary != null && dictionary.Count > 0)
                foreach (var obj in dictionary)
                {
                    var id = obj.Key;
                    var customObj = (Dictionary<string, object>)obj.Value;

                    var c = CreateCustomObject();
                    c.values["ID"] = customObj["ID"];
                    c.values["Shape"] = customObj["Shape"];
                    c.values["Position"] = customObj["Position"];
                    c.values["Scale"] = customObj["Scale"];
                    c.values["Rotation"] = customObj["Rotation"];
                    c.values["Color"] = customObj["Color"];
                    c.values["Custom Color"] = customObj["Custom Color"];
                    c.values["Opacity"] = customObj["Opacity"];
                    c.values["Parent"] = customObj["Parent"];
                    c.values["Parent Position Offset"] = customObj["Parent Position Offset"];
                    c.values["Parent Scale Offset"] = customObj["Parent Scale Offset"];
                    c.values["Parent Rotation Offset"] = customObj["Parent Rotation Offset"];
                    c.values["Parent Scale Active"] = customObj["Parent Scale Active"];
                    c.values["Parent Rotation Active"] = customObj["Parent Rotation Active"];
                    c.values["Depth"] = customObj["Depth"];
                    c.values["Visibility"] = customObj["Visibility"];
                    c.values["Visibility Value"] = customObj["Visibility Value"];
                    c.values["Visibility Not"] = customObj["Visibility Not"];

                    customObjects.Add((string)customObj["ID"], c);
                }

            updateCustomObjects();
        }

        public PlayerObject CreateCustomObject()
        {
            var obj = new PlayerObject();

            if (PlayerPlugin.debug)
                Debug.LogFormat("{0}Creating new PlayerObject!", PlayerPlugin.className);

            obj.name = "Object";
            obj.values = new Dictionary<string, object>();
            obj.values.Add("Shape", new Vector2Int(0, 0));
            obj.values.Add("Position", new Vector2(0f, 0f));
            obj.values.Add("Scale", new Vector2(1f, 1f));
            obj.values.Add("Rotation", 0f);
            obj.values.Add("Color", 0);
            obj.values.Add("Custom Color", "FFFFFF");
            obj.values.Add("Opacity", 0f);
            obj.values.Add("Parent", 0);
            obj.values.Add("Parent Position Offset", 1f);
            obj.values.Add("Parent Scale Offset", 1f);
            obj.values.Add("Parent Rotation Offset", 1f);
            obj.values.Add("Parent Scale Active", false);
            obj.values.Add("Parent Rotation Active", true);
            obj.values.Add("Depth", 0f);
            obj.values.Add("MeshRenderer", null);
            obj.values.Add("Visibility", 0);
            obj.values.Add("Visibility Value", 100f);
            obj.values.Add("Visibility Not", false);
            var id = LSText.randomNumString(16);
            obj.values.Add("ID", id);

            if (PlayerPlugin.debug)
                Debug.LogFormat("{0}Created new PlayerObject with id {1}!", PlayerPlugin.className, id);

            return obj;
        }

        void UpdateCustomTheme()
        {
            if (customObjects.Count > 0)
                foreach (var obj in customObjects.Values)
                {
                    int vis = (int)obj.values["Visibility"];
                    bool not = (bool)obj.values["Visibility Not"];
                    float value = (float)obj.values["Visibility Value"];
                    if (obj.gameObject != null)
                    {
                        switch (vis)
                        {
                            case 0:
                                {
                                    obj.gameObject.SetActive(true);
                                    break;
                                }
                            case 1:
                                {
                                    if (!not)
                                        obj.gameObject.SetActive(isBoosting);
                                    else
                                        obj.gameObject.SetActive(!isBoosting);
                                    break;
                                }
                            case 2:
                                {
                                    if (!not)
                                        obj.gameObject.SetActive(isTakingHit);
                                    else
                                        obj.gameObject.SetActive(!isTakingHit);
                                    break;
                                }
                            case 3:
                                {
                                    bool zen = DataManager.inst.GetSettingEnum("ArcadeDifficulty", 1) == 0 && EditorManager.inst == null || PlayerPlugin.ZenModeInEditor.Value;
                                    if (!not)
                                        obj.gameObject.SetActive(zen);
                                    else
                                        obj.gameObject.SetActive(!zen);
                                    break;
                                }
                            case 4:
                                {
                                    if (!not)
                                        obj.gameObject.SetActive((float)CustomPlayer.health / (float)initialHealthCount * 100f >= value);
                                    else
                                        obj.gameObject.SetActive(!((float)CustomPlayer.health / (float)initialHealthCount * 100f >= value));
                                    break;
                                }
                            case 5:
                                {
                                    if (!not)
                                        obj.gameObject.SetActive(CustomPlayer.health >= value);
                                    else
                                        obj.gameObject.SetActive(!(CustomPlayer.health >= value));
                                    break;
                                }
                            case 6:
                                {
                                    if (!not)
                                        obj.gameObject.SetActive(CustomPlayer.health == value);
                                    else
                                        obj.gameObject.SetActive(!(CustomPlayer.health == value));
                                    break;
                                }
                            case 7:
                                {
                                    if (!not)
                                        obj.gameObject.SetActive(CustomPlayer.health > value);
                                    else
                                        obj.gameObject.SetActive(!(CustomPlayer.health > value));
                                    break;
                                }
                            case 8:
                                {
                                    bool active = Input.GetKey(GetKeyCode((int)value));
                                    if (!not)
                                        obj.gameObject.SetActive(active);
                                    else
                                        obj.gameObject.SetActive(!active);
                                    break;
                                }
                        }
                    }

                    int col = (int)obj.values["Color"];
                    string hex = (string)obj.values["Custom Color"];
                    float alpha = (float)obj.values["Opacity"];
                    if (((MeshRenderer)obj.values["MeshRenderer"]) != null && obj.gameObject.activeSelf)
                    {
                        ((MeshRenderer)obj.values["MeshRenderer"]).material.color = GetColor(col, alpha, hex);
                    }
                }
        }

        public void UpdateTail(int _health, Vector3 _pos)
        {
            Debug.LogFormat("{0}Update Tail: Player: [{1}] - Health: [{2}]", PlayerPlugin.className, playerIndex, _health);
            for (int i = 2; i < path.Count; i++)
            {
                if (path[i].transform != null)
                {
                    if (i - 1 > _health)
                    {
                        if (path[i].transform.childCount != 0)
                            path[i].transform.GetChild(0).gameObject.SetActive(false);
                        else
                            path[i].transform.gameObject.SetActive(false);
                    }
                    else
                    {
                        if (path[i].transform.childCount != 0)
                            path[i].transform.GetChild(0).gameObject.SetActive(true);
                        else
                            path[i].transform.gameObject.SetActive(true);
                    }
                }
            }

            var currentModel = PlayerPlugin.CurrentModel(playerIndex);

            if (healthObjects.Count > 0)
            {
                for (int i = 0; i < healthObjects.Count; i++)
                {
                    if (i < _health && (bool)currentModel.values["GUI Health Active"] && (int)currentModel.values["GUI Health Mode"] == 0)
                        healthObjects[i].gameObject.SetActive(true);
                    else
                        healthObjects[i].gameObject.SetActive(false);
                }
            }

            var text = health.GetComponent<Text>();
            if ((bool)currentModel.values["GUI Health Active"] && ((int)currentModel.values["GUI Health Mode"] == 1 || (int)currentModel.values["GUI Health Mode"] == 2))
            {
                text.enabled = true;
                if ((int)currentModel.values["GUI Health Mode"] == 1)
                    text.text = _health.ToString();
                else
                    text.text = PlayerExtensions.ConvertHealthToEquals(_health, initialHealthCount);
            }
            else
            {
                text.enabled = false;
            }
            if ((bool)currentModel.values["GUI Health Active"] && (int)currentModel.values["GUI Health Mode"] == 3)
            {
                barBaseIm.gameObject.SetActive(true);
                var e = (float)_health / (float)initialHealthCount;
                barRT.sizeDelta = new Vector2(200f * e, 32f);
            }
            else
            {
                barBaseIm.gameObject.SetActive(false);
            }
            //for (int j = 1; j < _health + 1; j++)
            //{
            //    if (path.Count > _health + 1 && path[j].transform != null)
            //        path[j].transform.gameObject.SetActive(true);
            //}
        }

        public Color GetColor(int col, float alpha, string hex)
        {
            if (col < 4)
                return LSColors.fadeColor(GameManager.inst.LiveTheme.playerColors[col], alpha);
            if (col == 4)
                return LSColors.fadeColor(GameManager.inst.LiveTheme.guiColor, alpha);
            if (col > 4 && col < 23)
                return LSColors.fadeColor(GameManager.inst.LiveTheme.objectColors[col - 5], alpha);
            if (col == 23)
                return LSColors.fadeColor(GameManager.inst.LiveTheme.playerColors[playerIndex % 4], alpha);
            if (col == 24)
            {
                return LSColors.fadeColor(LSColors.HexToColor(hex), alpha);
            }

            return LSColors.pink500;
        }
        
        public Color GetColor(int col, string hex)
        {
            if (col < 4)
                return GameManager.inst.LiveTheme.playerColors[col];
            if (col == 4)
                return GameManager.inst.LiveTheme.guiColor;
            if (col > 4 && col < 23)
                return GameManager.inst.LiveTheme.objectColors[col - 5];
            if (col == 23)
                return GameManager.inst.LiveTheme.playerColors[playerIndex % 4];
            if (col == 24)
            {
                return LSColors.HexToColor(hex);
            }

            return LSColors.pink500;
        }

        #endregion

        #region Actions

        void CreatePulse()
        {
            var currentModel = PlayerPlugin.CurrentModel(playerIndex);

            if (currentModel == null || !currentModel.values.ContainsKey("Pulse Active") || !(bool)currentModel.values["Pulse Active"])
            {
                return;
            }

            var player = playerObjects["RB Parent"].gameObject;

            int s = Mathf.Clamp(((Vector2Int)currentModel.values["Pulse Shape"]).x, 0, ObjectManager.inst.objectPrefabs.Count - 1);
            int so = Mathf.Clamp(((Vector2Int)currentModel.values["Pulse Shape"]).y, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);

            var objcopy = ObjectManager.inst.objectPrefabs[s].options[so];
            if (s == 4 || s == 6)
            {
                objcopy = ObjectManager.inst.objectPrefabs[0].options[0];
            }

            var pulse = Instantiate(objcopy);
            pulse.transform.SetParent(ObjectManager.inst.objectParent.transform);
            pulse.transform.localScale = new Vector3(((Vector2)currentModel.values["Pulse Start Scale"]).x, ((Vector2)currentModel.values["Pulse Start Scale"]).y, 1f);
            pulse.transform.position = player.transform.position;
            pulse.transform.GetChild(0).localPosition = new Vector3(((Vector2)currentModel.values["Pulse Start Position"]).x, ((Vector2)currentModel.values["Pulse Start Position"]).y, (float)currentModel.values["Pulse Depth"]);
            pulse.transform.GetChild(0).localRotation = Quaternion.Euler(new Vector3(0f, 0f, (float)currentModel.values["Pulse Start Rotation"]));

            if ((bool)currentModel.values["Pulse Rotate to Head"])
            {
                pulse.transform.localRotation = player.transform.localRotation;
            }

            //Destroy
            {
                if (pulse.transform.GetChild(0).GetComponent<SelectObjectInEditor>())
                {
                    Destroy(pulse.transform.GetChild(0).GetComponent<SelectObjectInEditor>());
                }
                if (pulse.transform.GetChild(0).GetComponent<BoxCollider2D>())
                {
                    Destroy(pulse.transform.GetChild(0).GetComponent<BoxCollider2D>());
                }
                if (pulse.transform.GetChild(0).GetComponent<PolygonCollider2D>())
                {
                    Destroy(pulse.transform.GetChild(0).GetComponent<PolygonCollider2D>());
                }
                if (pulse.transform.GetChild(0).gameObject.GetComponentByName("RTObject"))
                {
                    Destroy(pulse.transform.GetChild(0).gameObject.GetComponentByName("RTObject"));
                }
            }

            var obj = new PlayerObject("Pulse", pulse.transform.GetChild(0).gameObject);

            MeshRenderer pulseRenderer = pulse.transform.GetChild(0).GetComponent<MeshRenderer>();
            obj.values.Add("MeshRenderer", pulseRenderer);
            obj.values.Add("Opacity", 0f);
            obj.values.Add("ColorTween", 0f);
            obj.values.Add("StartColor", (int)currentModel.values["Pulse Start Color"]);
            obj.values.Add("EndColor", (int)currentModel.values["Pulse End Color"]);
            obj.values.Add("StartCustomColor", (string)currentModel.values["Pulse Start Custom Color"]);
            obj.values.Add("EndCustomColor", (string)currentModel.values["Pulse End Custom Color"]);

            boosts.Add(obj);

            pulseRenderer.enabled = true;
            pulseRenderer.material = ((MeshRenderer)playerObjects["Head"].values["MeshRenderer"]).material;
            pulseRenderer.material.shader = ((MeshRenderer)playerObjects["Head"].values["MeshRenderer"]).material.shader;
            Color colorBase = ((MeshRenderer)playerObjects["Head"].values["MeshRenderer"]).material.color;

            int easingPos = (int)currentModel.values["Pulse Easing Position"];
            int easingSca = (int)currentModel.values["Pulse Easing Scale"];
            int easingRot = (int)currentModel.values["Pulse Easing Rotation"];
            int easingOpa = (int)currentModel.values["Pulse Easing Opacity"];
            int easingCol = (int)currentModel.values["Pulse Easing Color"];

            float duration = Mathf.Clamp((float)currentModel.values["Pulse Duration"], 0.001f, 20f) / PlayerExtensions.Pitch;

            pulse.transform.GetChild(0).DOLocalMove(new Vector3(((Vector2)currentModel.values["Pulse End Position"]).x, ((Vector2)currentModel.values["Pulse End Position"]).y, (float)currentModel.values["Pulse Depth"]), duration).SetEase(DataManager.inst.AnimationList[easingPos].Animation);
            var tweenScale = pulse.transform.DOScale(new Vector3(((Vector2)currentModel.values["Pulse End Scale"]).x, ((Vector2)currentModel.values["Pulse End Scale"]).y, 1f), duration).SetEase(DataManager.inst.AnimationList[easingSca].Animation);
            pulse.transform.GetChild(0).DOLocalRotate(new Vector3(0f, 0f, (float)currentModel.values["Pulse End Rotation"]), duration).SetEase(DataManager.inst.AnimationList[easingRot].Animation);

            DOTween.To(delegate (float x)
            {
                obj.values["Opacity"] = x;
            }, (float)currentModel.values["Pulse Start Opacity"], (float)currentModel.values["Pulse End Opacity"], duration).SetEase(DataManager.inst.AnimationList[easingOpa].Animation);
            DOTween.To(delegate (float x)
            {
                obj.values["ColorTween"] = x;
            }, 0f, 1f, duration).SetEase(DataManager.inst.AnimationList[easingCol].Animation);

            tweenScale.OnComplete(delegate ()
            {
                Destroy(pulse);
                boosts.Remove(obj);
            });
        }

        void UpdateBoostTheme()
        {
            if (boosts.Count > 0)
            {
                foreach (var boost in boosts)
                {
                    if (boost != null)
                    {
                        int startCol = (int)boost.values["StartColor"];
                        int endCol = (int)boost.values["EndColor"];

                        var startHex = (string)boost.values["StartCustomColor"];
                        var endHex = (string)boost.values["EndCustomColor"];

                        float alpha = (float)boost.values["Opacity"];
                        float colorTween = (float)boost.values["ColorTween"];

                        Color startColor = GetColor(startCol, alpha, startHex);
                        Color endColor = GetColor(endCol, alpha, endHex);

                        if (((MeshRenderer)boost.values["MeshRenderer"]) != null)
                        {
                            ((MeshRenderer)boost.values["MeshRenderer"]).material.color = Color.Lerp(startColor, endColor, colorTween);
                        }
                    }
                }
            }
        }

        public List<PlayerObject> boosts = new List<PlayerObject>();

        void PlaySound(AudioClip _clip, float pitch = 1f)
        {
            float p = pitch * PlayerExtensions.Pitch;

            AudioSource audioSource = Camera.main.gameObject.AddComponent<AudioSource>();
            audioSource.clip = _clip;
            audioSource.playOnAwake = true;
            audioSource.loop = false;
            audioSource.volume = AudioManager.inst.sfxVol;
            audioSource.pitch = pitch * AudioManager.inst.pitch;
            audioSource.Play();
            StartCoroutine(AudioManager.inst.DestroyWithDelay(audioSource, _clip.length / p));
        }

        void CreateBullet()
        {
            if (PlayerPlugin.PlayerShootSound.Value)
                PlaySound(AudioManager.inst.GetSound("boost"), 0.7f);

            canShoot = false;

            var currentModel = PlayerPlugin.CurrentModel(playerIndex);

            if (currentModel == null || !currentModel.values.ContainsKey("Bullet Active") || !(bool)currentModel.values["Bullet Active"])
            {
                Debug.LogFormat("{0}Cannot create bullet because of the following reasons:\nCurrent Model is null = {1}\nCurrent Model does not contain Bullet Key = {2}\nBullet is not active = {1}",
                    currentModel == null, currentModel != null && !currentModel.values.ContainsKey("Bullet Active"), currentModel != null && currentModel.values.ContainsKey("Bullet Active") && !(bool)currentModel.values["Bullet Active"]);
                return;
            }

            var player = playerObjects["RB Parent"].gameObject;

            int s = Mathf.Clamp(((Vector2Int)currentModel.values["Bullet Shape"]).x, 0, ObjectManager.inst.objectPrefabs.Count - 1);
            int so = Mathf.Clamp(((Vector2Int)currentModel.values["Bullet Shape"]).y, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);

            var objcopy = ObjectManager.inst.objectPrefabs[s].options[so];
            if (s == 4 || s == 6)
            {
                objcopy = ObjectManager.inst.objectPrefabs[0].options[0];
            }

            var pulse = Instantiate(objcopy);
            pulse.transform.SetParent(ObjectManager.inst.objectParent.transform);
            pulse.transform.localScale = new Vector3(((Vector2)currentModel.values["Bullet Start Scale"]).x, ((Vector2)currentModel.values["Bullet Start Scale"]).y, 1f);

            var vec = new Vector3(((Vector2)currentModel.values["Bullet Origin"]).x, ((Vector2)currentModel.values["Bullet Origin"]).y, 0f);
            if (rotateMode == RotateMode.FlipX && lastMovement.x < 0f)
                vec.x = -vec.x;

            pulse.transform.position = player.transform.position + vec;
            pulse.transform.GetChild(0).localPosition = new Vector3(((Vector2)currentModel.values["Bullet Start Position"]).x, ((Vector2)currentModel.values["Bullet Start Position"]).y, (float)currentModel.values["Bullet Depth"]);
            pulse.transform.GetChild(0).localRotation = Quaternion.Euler(new Vector3(0f, 0f, (float)currentModel.values["Bullet Start Rotation"]));

            if (!PlayerPlugin.AllowPlayersToTakeBulletDamage.Value || !(bool)currentModel.values["Bullet Hurt Players"])
            {
                pulse.tag = "Helper";
                pulse.transform.GetChild(0).tag = "Helper";
            }

            pulse.transform.GetChild(0).gameObject.name = "bullet (Player " + (playerIndex + 1).ToString() + ")";

            float speed = Mathf.Clamp((float)currentModel.values["Bullet Speed Amount"], 0.001f, 20f) / PlayerExtensions.Pitch;
            var b = pulse.AddComponent<Bullet>();
            b.speed = speed;
            b.player = this;
            b.Assign();

            pulse.transform.localRotation = player.transform.localRotation;

            //Destroy
            {
                if (pulse.transform.GetChild(0).GetComponent<SelectObjectInEditor>())
                {
                    Destroy(pulse.transform.GetChild(0).GetComponent<SelectObjectInEditor>());
                }
                if (pulse.transform.GetChild(0).gameObject.GetComponentByName("RTObject"))
                {
                    Destroy(pulse.transform.GetChild(0).gameObject.GetComponentByName("RTObject"));
                }
            }

            var obj = new PlayerObject("Bullet", pulse.transform.GetChild(0).gameObject);

            MeshRenderer pulseRenderer = pulse.transform.GetChild(0).GetComponent<MeshRenderer>();
            obj.values.Add("MeshRenderer", pulseRenderer);
            obj.values.Add("Opacity", 0f);
            obj.values.Add("ColorTween", 0f);
            obj.values.Add("StartColor", (int)currentModel.values["Bullet Start Color"]);
            obj.values.Add("EndColor", (int)currentModel.values["Bullet End Color"]);
            obj.values.Add("StartCustomColor", (string)currentModel.values["Bullet Start Custom Color"]);
            obj.values.Add("EndCustomColor", (string)currentModel.values["Bullet End Custom Color"]);

            boosts.Add(obj);

            pulseRenderer.enabled = true;
            pulseRenderer.material = ((MeshRenderer)playerObjects["Head"].values["MeshRenderer"]).material;
            pulseRenderer.material.shader = ((MeshRenderer)playerObjects["Head"].values["MeshRenderer"]).material.shader;
            Color colorBase = ((MeshRenderer)playerObjects["Head"].values["MeshRenderer"]).material.color;

            var collider2D = pulse.transform.GetChild(0).GetComponent<Collider2D>();
            collider2D.enabled = true;
            //collider2D.isTrigger = false;

            var rb2D = pulse.transform.GetChild(0).gameObject.AddComponent<Rigidbody2D>();
            rb2D.gravityScale = 0f;

            var bulletCollider = pulse.transform.GetChild(0).gameObject.AddComponent<BulletCollider>();
            bulletCollider.rb = (Rigidbody2D)playerObjects["RB Parent"].values["Rigidbody2D"];
            if (currentModel.values.ContainsKey("Bullet AutoKill"))
            {
                bulletCollider.kill = (bool)currentModel.values["Bullet AutoKill"];
            }
            bulletCollider.player = this;
            bulletCollider.playerObject = obj;

            int easingPos = (int)currentModel.values["Bullet Easing Position"];
            int easingSca = (int)currentModel.values["Bullet Easing Scale"];
            int easingRot = (int)currentModel.values["Bullet Easing Rotation"];
            int easingOpa = (int)currentModel.values["Bullet Easing Opacity"];
            int easingCol = (int)currentModel.values["Bullet Easing Color"];

            float posDuration = Mathf.Clamp((float)currentModel.values["Bullet Duration Position"], 0.001f, 20f) / PlayerExtensions.Pitch;
            float scaDuration = Mathf.Clamp((float)currentModel.values["Bullet Duration Scale"], 0.001f, 20f) / PlayerExtensions.Pitch;
            float rotDuration = Mathf.Clamp((float)currentModel.values["Bullet Duration Rotation"], 0.001f, 20f) / PlayerExtensions.Pitch;
            float lifeTime = Mathf.Clamp((float)currentModel.values["Bullet Lifetime"], 0.001f, 20f) / PlayerExtensions.Pitch;

            pulse.transform.GetChild(0).DOLocalMove(new Vector3(((Vector2)currentModel.values["Bullet End Position"]).x, ((Vector2)currentModel.values["Bullet End Position"]).y, (float)currentModel.values["Bullet Depth"]), posDuration).SetEase(DataManager.inst.AnimationList[easingPos].Animation);
            pulse.transform.DOScale(new Vector3(((Vector2)currentModel.values["Bullet End Scale"]).x, ((Vector2)currentModel.values["Bullet End Scale"]).y, 1f), scaDuration).SetEase(DataManager.inst.AnimationList[easingSca].Animation);
            pulse.transform.GetChild(0).DOLocalRotate(new Vector3(0f, 0f, (float)currentModel.values["Bullet End Rotation"]), rotDuration).SetEase(DataManager.inst.AnimationList[easingRot].Animation);

            DOTween.To(delegate (float x)
            {
                obj.values["Opacity"] = x;
            }, (float)currentModel.values["Bullet Start Opacity"], (float)currentModel.values["Bullet End Opacity"], posDuration).SetEase(DataManager.inst.AnimationList[easingOpa].Animation);
            DOTween.To(delegate (float x)
            {
                obj.values["ColorTween"] = x;
            }, 0f, 1f, posDuration).SetEase(DataManager.inst.AnimationList[easingCol].Animation);

            StartCoroutine(CanShoot());

            var tweener = DOTween.To(delegate (float x) { }, 1f, 1f, lifeTime).SetEase(DataManager.inst.AnimationList[easingOpa].Animation);
            bulletCollider.tweener = tweener;

            tweener.OnComplete(delegate ()
            {
                var tweenScale = pulse.transform.GetChild(0).DOScale(Vector3.zero, 0.2f).SetEase(DataManager.inst.AnimationList[2].Animation);
                bulletCollider.tweener = tweenScale;

                tweenScale.OnComplete(delegate ()
                {
                    Destroy(pulse);
                    boosts.Remove(obj);
                    obj = null;
                });
            });
        }

        IEnumerator CanShoot()
        {
            var currentModel = PlayerPlugin.CurrentModel(playerIndex);
            if (currentModel != null)
            {
                var delay = (float)currentModel.values["Bullet Delay Amount"];
                yield return new WaitForSeconds(delay);
                canShoot = true;
            }

            yield break;
        }

        #endregion

        #region Code

        public string SpawnCodePath => "player/spawn.cs";
        public string BoostCodePath => "player/boost.cs";
        public string HitCodePath => "player/hit.cs";
        public string DeathCodePath => "player/death.cs";

        void EvaluateSpawnCode()
        {
            if (!PlayerPlugin.EvaluateCode.Value)
                return;

            string path = RTFile.basePath + SpawnCodePath;

            if (RTFile.FileExists(RTFile.ApplicationDirectory + path))
            {
                var def = $"var playerIndex = {playerIndex};{Environment.NewLine}";

                string cs = FileManager.inst.LoadJSONFile(RTFile.basePath + path);

                if (!cs.Contains("System.IO.File.") && !cs.Contains("File."))
                    RTCode.Evaluate($"{def}{cs}");
            }
        }

        void EvaluateBoostCode()
        {
            if (!PlayerPlugin.EvaluateCode.Value)
                return;

            string path = RTFile.basePath + BoostCodePath;

            if (RTFile.FileExists(RTFile.ApplicationDirectory + path))
            {
                var def = $"var playerIndex = {playerIndex};{Environment.NewLine}";

                string cs = FileManager.inst.LoadJSONFile(RTFile.basePath + path);

                if (!cs.Contains("System.IO.File.") && !cs.Contains("File."))
                    RTCode.Evaluate($"{def}{cs}");
            }
        }

        void EvaluateHitCode()
        {
            if (!PlayerPlugin.EvaluateCode.Value)
                return;

            string path = RTFile.basePath + HitCodePath;

            if (RTFile.FileExists(RTFile.ApplicationDirectory + path))
            {
                var def = $"var playerIndex = {playerIndex};{Environment.NewLine}";

                string cs = FileManager.inst.LoadJSONFile(RTFile.basePath + path);

                if (!cs.Contains("System.IO.File.") && !cs.Contains("File."))
                    RTCode.Evaluate($"{def}{cs}");
            }
        }

        void EvaluateDeathCode()
        {
            if (!PlayerPlugin.EvaluateCode.Value)
                return;

            string path = RTFile.basePath + DeathCodePath;

            if (RTFile.FileExists(RTFile.ApplicationDirectory + path))
            {
                var def = $"var playerIndex = {playerIndex};{Environment.NewLine}";

                string cs = FileManager.inst.LoadJSONFile(RTFile.basePath + path);

                if (!cs.Contains("System.IO.File.") && !cs.Contains("File."))
                    RTCode.Evaluate($"{def}{cs}");
            }
        }
        
        #endregion

        public KeyCode GetKeyCode(int key)
        {
            if (key < 91)
            switch (key)
            {
                case 0: return KeyCode.None;
                case 1: return KeyCode.Backspace;
                case 2: return KeyCode.Tab;
                case 3: return KeyCode.Clear;
                case 4: return KeyCode.Return;
                case 5: return KeyCode.Pause;
                case 6: return KeyCode.Escape;
                case 7: return KeyCode.Space;
                case 8: return KeyCode.Quote;
                case 9: return KeyCode.Comma;
                case 10: return KeyCode.Minus;
                case 11: return KeyCode.Period;
                case 12: return KeyCode.Slash;
                case 13: return KeyCode.Alpha0;
                case 14: return KeyCode.Alpha1;
                case 15: return KeyCode.Alpha2;
                case 16: return KeyCode.Alpha3;
                case 17: return KeyCode.Alpha4;
                case 18: return KeyCode.Alpha5;
                case 19: return KeyCode.Alpha6;
                case 20: return KeyCode.Alpha7;
                case 21: return KeyCode.Alpha8;
                case 22: return KeyCode.Alpha9;
                case 23: return KeyCode.Semicolon;
                case 24: return KeyCode.Equals;
                case 25: return KeyCode.LeftBracket;
                case 26: return KeyCode.RightBracket;
                case 27: return KeyCode.Backslash;
                case 28: return KeyCode.A;
                case 29: return KeyCode.B;
                case 30: return KeyCode.C;
                case 31: return KeyCode.D;
                case 32: return KeyCode.E;
                case 33: return KeyCode.F;
                case 34: return KeyCode.G;
                case 35: return KeyCode.H;
                case 36: return KeyCode.I;
                case 37: return KeyCode.J;
                case 38: return KeyCode.K;
                case 39: return KeyCode.L;
                case 40: return KeyCode.M;
                case 41: return KeyCode.N;
                case 42: return KeyCode.O;
                case 43: return KeyCode.P;
                case 44: return KeyCode.Q;
                case 45: return KeyCode.R;
                case 46: return KeyCode.S;
                case 47: return KeyCode.T;
                case 48: return KeyCode.U;
                case 49: return KeyCode.V;
                case 50: return KeyCode.W;
                case 51: return KeyCode.X;
                case 52: return KeyCode.Y;
                case 53: return KeyCode.Z;
                case 54: return KeyCode.Keypad0;
                case 55: return KeyCode.Keypad1;
                case 56: return KeyCode.Keypad2;
                case 57: return KeyCode.Keypad3;
                case 58: return KeyCode.Keypad4;
                case 59: return KeyCode.Keypad5;
                case 60: return KeyCode.Keypad6;
                case 61: return KeyCode.Keypad7;
                case 62: return KeyCode.Keypad8;
                case 63: return KeyCode.Keypad9;
                case 64: return KeyCode.KeypadDivide;
                case 65: return KeyCode.KeypadMultiply;
                case 66: return KeyCode.KeypadMinus;
                case 67: return KeyCode.KeypadPlus;
                case 68: return KeyCode.KeypadEnter;
                case 69: return KeyCode.UpArrow;
                case 70: return KeyCode.DownArrow;
                case 71: return KeyCode.RightArrow;
                case 72: return KeyCode.LeftArrow;
                case 73: return KeyCode.Insert;
                case 74: return KeyCode.Home;
                case 75: return KeyCode.End;
                case 76: return KeyCode.PageUp;
                case 77: return KeyCode.PageDown;
                case 78: return KeyCode.RightShift;
                case 79: return KeyCode.LeftShift;
                case 80: return KeyCode.RightControl;
                case 81: return KeyCode.LeftControl;
                case 82: return KeyCode.RightAlt;
                case 83: return KeyCode.LeftAlt;
                case 84: return KeyCode.Mouse0;
                case 85: return KeyCode.Mouse1;
                case 86: return KeyCode.Mouse2;
                case 87: return KeyCode.Mouse3;
                case 88: return KeyCode.Mouse4;
                case 89: return KeyCode.Mouse5;
                case 90: return KeyCode.Mouse6;
            }

            if (key > 90)
            {
                int num = key + 259;

                if (IndexToInt(CustomPlayer.playerIndex) > 0)
                {
                    string str = (IndexToInt(CustomPlayer.playerIndex) * 2).ToString() + "0";
                    num += int.Parse(str);
                }

                return (KeyCode)num;
            }

            return KeyCode.None;
        }

        public int IndexToInt(PlayerIndex playerIndex)
        {
            if (playerIndex == PlayerIndex.One)
                return 0;
            if (playerIndex == PlayerIndex.Two)
                return 1;
            if (playerIndex == PlayerIndex.Three)
                return 2;
            if (playerIndex == PlayerIndex.Four)
                return 3;
            if (playerIndex == (PlayerIndex)4)
                return 4;
            if (playerIndex == (PlayerIndex)5)
                return 5;
            if (playerIndex == (PlayerIndex)6)
                return 6;
            if (playerIndex == (PlayerIndex)7)
                return 7;

            return 0;
        }

        #region Objects

        public Dictionary<string, PlayerObject> customObjects = new Dictionary<string, PlayerObject>();
        public Dictionary<string, PlayerObject> playerObjects = new Dictionary<string, PlayerObject>();

        public class PlayerObject
        {
            public PlayerObject()
            {

            }

            public PlayerObject(string _name, GameObject _gm)
            {
                name = _name;
                gameObject = _gm;
                values = new Dictionary<string, object>();

                values.Add("Position", Vector3.zero);
                values.Add("Scale", Vector3.one);
                values.Add("Rotation", 0f);
                values.Add("Color", 0);
            }
            public PlayerObject(string _name, Dictionary<string, object> _values, GameObject _gm)
            {
                name = _name;
                values = _values;
                gameObject = _gm;
            }

            public string name;
            public GameObject gameObject;

            public Dictionary<string, object> values;
        }

        public List<MovementPath> path = new List<MovementPath>();

        public class MovementPath
        {
            public MovementPath(Vector3 _pos, Quaternion _rot, Transform _tf)
            {
                pos = _pos;
                rot = _rot;
                transform = _tf;
            }

            public MovementPath(Vector3 _pos, Quaternion _rot, Transform _tf, bool active)
            {
                pos = _pos;
                rot = _rot;
                transform = _tf;
                this.active = active;
            }

            public bool active = true;

            public Vector3 lastPos;
            public Vector3 pos;

            public Quaternion rot;

            public Transform transform;
        }

        public List<HealthObject> healthObjects = new List<HealthObject>();

        public class HealthObject
        {
            public HealthObject(GameObject gameObject, Image image)
            {
                this.gameObject = gameObject;
                this.image = image;
            }

            public GameObject gameObject;
            public Image image;
        }

        #endregion
    }
}
