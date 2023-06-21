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

using DG.Tweening;

using LSFunctions;

using CreativePlayers.Functions;
using CreativePlayers.Functions.Components;

namespace CreativePlayers
{
    public class RTPlayer : MonoBehaviour
    {
        public MyGameActions Actions { get; set; }

        public bool canBoost = true;
        public bool canMove = true;
        public bool canTakeDamage;

        private Vector3 lastPos;
        private float lastMoveHorizontal;
        private float lastMoveVertical;
        private Vector3 lastVelocity;

        public bool isTakingHit;
        public bool isBoosting;
        public bool isBoostCancelled;
        public bool isDead = true;

        private float startHurtTime;
        private float startBoostTime;
        public float maxBoostTime = 0.18f;
        public float minBoostTime = 0.07f;
        public float boostCooldown = 0.1f;
        public float idleSpeed = 20f;
        public float boostSpeed = 85f;

        public int playerIndex;
        public float tailDistance = 2f;
        public int tailMode;

        public Coroutine boostCoroutine;

        public bool includeNegativeZoom = false;

        public bool CanTakeDamage
        {
            get
            {
                return (!(EditorManager.inst == null) || DataManager.inst.GetSettingEnum("ArcadeDifficulty", 1) != 0 || !DataManager.inst.GetSettingBool("IsArcade")) && (!(EditorManager.inst == null) || GameManager.inst.gameState != GameManager.State.Paused) && (!(EditorManager.inst != null) || !EditorManager.inst.isEditing) && canTakeDamage;
            }
            set
            {
                canTakeDamage = value;
            }
        }

        public bool CanMove
        {
            get
            {
                return canMove;
            }
            set
            {
                canMove = value;
            }
        }

        public bool CanBoost
        {
            get
            {
                return (!(EditorManager.inst != null) || !EditorManager.inst.isEditing) && (canBoost && !isBoosting) && (GameManager.inst == null || GameManager.inst.gameState != GameManager.State.Paused) && !LSHelpers.IsUsingInputField();
            }
            set
            {
                canBoost = value;
            }
        }

        public bool PlayerAlive
        {
            get
            {
                return (!(InputDataManager.inst != null) || InputDataManager.inst.players.Count > 0) && (InputDataManager.inst != null && InputDataManager.inst.players[playerIndex].health > 0) && !isDead;
            }
        }

        //Player Parent Tree (original):
        //player-complete (has Player component)
        //player-complete/Player
        //player-complete/Player/Player
        //player-complete/Player/Player/death-explosion
        //player-complete/Player/Player/burst-explosion
        //player-complete/Player/Player/spawn-implosion
        //player-complete/Player/boost
        //player-complete/trail (has PlayerTrail component)
        //player-complete/trail/1
        //player-complete/trail/2
        //player-complete/trail/3

        public delegate void PlayerHitDelegate(int _health, Vector3 _pos);

        public delegate void PlayerHealDelegate(int _health, Vector3 _pos);

        public delegate void PlayerBoostDelegate();

        public delegate void PlayerDeathDelegate(Vector3 _pos);

        public event PlayerHitDelegate playerHitEvent;

        public event PlayerHealDelegate playerHealEvent;

        public event PlayerBoostDelegate playerBoostEvent;

        public event PlayerDeathDelegate playerDeathEvent;
        public TailUpdateMode updateMode = TailUpdateMode.FixedUpdate;
        public enum TailUpdateMode
        {
            Update,
            FixedUpdate,
            LateUpdate
        }

        private void Awake()
        {
            //if (gameObject.GetComponent<Player>())
            //    Destroy(gameObject.GetComponent<Player>());
            if (transform.Find("trail").GetComponent<PlayerTrail>())
                Destroy(transform.Find("trail").GetComponent<PlayerTrail>());

            playerObjects.Add("Base", new PlayerObject("Base", gameObject));
            playerObjects["Base"].values.Add("Transform", gameObject.transform);
            playerObjects["Base"].values.Add("Animator", gameObject.GetComponent<Animator>());

            var rb = transform.Find("Player").gameObject;
            playerObjects.Add("RB Parent", new PlayerObject("RB Parent", rb));
            playerObjects["RB Parent"].values.Add("Transform", rb.transform);
            playerObjects["RB Parent"].values.Add("Rigidbody2D", rb.GetComponent<Rigidbody2D>());
            playerObjects["RB Parent"].values.Add("OnTriggerEnterPass", rb.GetComponent<OnTriggerEnterPass>());

            var circleCollider = rb.GetComponent<CircleCollider2D>();

            circleCollider.enabled = false;

            var polygonCollider = rb.AddComponent<PolygonCollider2D>();

            playerObjects["RB Parent"].values.Add("CircleCollider2D", circleCollider);
            playerObjects["RB Parent"].values.Add("PlayerSelector", rb.AddComponent<PlayerSelector>());
            ((PlayerSelector)playerObjects["RB Parent"].values["PlayerSelector"]).id = playerIndex;

            var head = transform.Find("Player/Player").gameObject;
            playerObjects.Add("Head", new PlayerObject("Head", head));

            var headMesh = head.GetComponent<MeshFilter>();

            playerObjects["Head"].values.Add("MeshFilter", headMesh);

            polygonCollider.CreateCollider(headMesh);

            playerObjects["Head"].values.Add("MeshRenderer", head.GetComponent<MeshRenderer>());

            polygonCollider.enabled = false;
            circleCollider.enabled = true;

            var boost = transform.Find("Player/boost").gameObject;
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
                transform.Find("Player/boost").SetParent(boostBase.transform);

                playerObjects.Add("Boost Base", new PlayerObject("Boost Base", boostBase));

                path.Add(new MovementPath(Vector3.zero, Quaternion.identity, rb.transform));
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
                playerObjects.Add("Tail Tracker", new PlayerObject("Tail Tracker", delayTarget));

                //NameTag
                canvas = RTExtensions.CreateCanvas("Name Tag Canvas");
                canvas.transform.SetParent(transform);
                GameObject e = new GameObject("Base");
                e.transform.SetParent(canvas.transform);
                var image = RTExtensions.GenerateUIImage("Image", e.transform);
                var text = RTExtensions.GenerateUIText("Text", ((GameObject)image["GameObject"]).transform);

                RTExtensions.SetRectTransform((RectTransform)image["RectTransform"], new Vector2(960f, 585f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(166f, 44f));
                var component = (Text)text["Text"];
                component.text = "Player " + (playerIndex + 1).ToString() + "[===]";
                component.alignment = TextAnchor.MiddleCenter;

                ((RectTransform)text["RectTransform"]).sizeDelta = new Vector2(200f, 100f);

                playerObjects.Add("NameTag Text", new PlayerObject("NameTag Text", (GameObject)text["GameObject"]));
                playerObjects.Add("NameTag Image", new PlayerObject("NameTag Image", (GameObject)image["GameObject"]));

                playerObjects["NameTag Text"].values.Add("Text", component);
                playerObjects["NameTag Image"].values.Add("Image", (Image)image["Image"]);

                //DelayTracker
                for (int i = 1; i < 4; i++)
                {
                    var tail = playerObjects[string.Format("Tail {0} Base", i)].gameObject;
                    var delayTracker = tail.AddComponent<DelayTracker>();
                    delayTracker.offset = -i * tailDistance / 2f;
                    delayTracker.followSharpness *= (-i + 4);
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

            updatePlayer();
        }

        public GameObject canvas;

        private void Start()
        {
            playerHealEvent += UpdateTail;
            playerHitEvent += UpdateTail;
            SetColor(LSColors.red500, LSColors.gray900);
            Spawn();
        }

        //15.4791 -4.9977 -162
        //15.4785 -4.997 -162
        //0.5733 -0.1851 -6


        private void Update()
        {
            UpdateCustomTheme();
            if (canvas != null)
            {
                bool act = EditorManager.inst == null || EditorManager.inst != null && !EditorManager.inst.isEditing;
                canvas.SetActive(act);
                if (act)
                {
                    Vector3 v = playerObjects["Head"].gameObject.transform.position + -(GameObject.Find("Camera Parent").transform.position + GameObject.Find("Camera Parent/cameras").transform.localPosition);
                    canvas.transform.Find("Base").position = v * 27f;
                    var text = (Text)playerObjects["NameTag Text"].values["Text"];
                    text.text = "Player " + (playerIndex + 1).ToString() + " " + RTExtensions.ConvertHealthToEquals(InputDataManager.inst.players[playerIndex].health);
                    text.color = GameManager.inst.LiveTheme.guiColor;

                    ((Image)playerObjects["NameTag Image"].values["Image"]).color = LSColors.fadeColor(GameManager.inst.LiveTheme.guiColor, 0.3f);
                }
            }

            if (updateMode == TailUpdateMode.Update)
            {
                UpdateTailDistance();
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
            }

            var currentModel = PlayerPlugin.playerModels[PlayerPlugin.playerModelsIndex[playerIndex]];
            if (playerObjects["Boost Trail"].values["TrailRenderer"] != null && currentModel != null && (bool)currentModel.values["Boost Trail Emitting"])
            {
                var tf = playerObjects["Boost"].gameObject.transform;
                Vector2 v = new Vector2(tf.localScale.x, tf.localScale.y);

                ((TrailRenderer)playerObjects["Boost Trail"].values["TrailRenderer"]).startWidth = (float)currentModel.values["Boost Trail Start Width"] * v.magnitude / 1.414213f;
                ((TrailRenderer)playerObjects["Boost Trail"].values["TrailRenderer"]).endWidth = (float)currentModel.values["Boost Trail End Width"] * v.magnitude / 1.414213f;
            }
        }

        private void FixedUpdate()
        {
            if (updateMode == TailUpdateMode.FixedUpdate)
            {
                UpdateTailDistance();
            }
        }

        private void LateUpdate()
        {
            if (updateMode == TailUpdateMode.LateUpdate)
            {
                UpdateTailDistance();
            }
            UpdateTailTransform();

            var rb = (Rigidbody2D)playerObjects["RB Parent"].values["Rigidbody2D"];
            var anim = (Animator)playerObjects["Base"].values["Animator"];
            var player = playerObjects["RB Parent"].gameObject;

            if (PlayerAlive && Actions != null && InputDataManager.inst.players[playerIndex].active && CanMove && (GameManager.inst == null || GameManager.inst.gameState != GameManager.State.Paused) && !LSHelpers.IsUsingInputField())
            {
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
                Vector3 vector = Vector3.zero;
                if (isBoosting)
                {
                    vector = new Vector3(lastMoveHorizontal, lastMoveVertical, 0f);
                    vector = vector.normalized;
                    rb.velocity = vector * boostSpeed * ((GameManager.inst != null) ? GameManager.inst.getPitch() : 1f);
                }
                else
                {
                    vector = new Vector3(x, y, 0f);
                    if (vector.magnitude > 1f)
                    {
                        vector = vector.normalized;
                    }
                    rb.velocity = vector * idleSpeed * ((GameManager.inst != null) ? GameManager.inst.getPitch() : 1f);
                }
                anim.SetFloat("Speed", Mathf.Abs(vector.x + vector.y));
                //Consider moving this outside to test.
                Quaternion b = Quaternion.AngleAxis(Mathf.Atan2(lastVelocity.y, lastVelocity.x) * 57.29578f, player.transform.forward);
                player.transform.rotation = Quaternion.Slerp(player.transform.rotation, b, 720f * Time.deltaTime);
                player.transform.eulerAngles = new Vector3(0f, 0f, player.transform.eulerAngles.z);

                if (rb.velocity != Vector2.zero)
                {
                    lastVelocity = rb.velocity;
                }

                player.transform.rotation = Quaternion.Euler(0f, 0f, player.transform.rotation.eulerAngles.z);
            }
            else if (CanMove)
            {
                rb.velocity = Vector3.zero;
            }
            Vector3 vector2 = Camera.main.WorldToViewportPoint(player.transform.position);
            vector2.x = Mathf.Clamp(vector2.x, 0f, 1f);
            vector2.y = Mathf.Clamp(vector2.y, 0f, 1f);
            if (Camera.main.orthographicSize > 0f && (includeNegativeZoom && Camera.main.orthographicSize < 0f || !includeNegativeZoom))
            {
                float maxDistanceDelta = Time.deltaTime * 1500f;
                player.transform.position = Vector3.MoveTowards(lastPos, Camera.main.ViewportToWorldPoint(vector2), maxDistanceDelta);
            }
            lastPos = player.transform.position;
        }

        public void UpdateTailDistance()
        {
            path[0].pos = ((Transform)playerObjects["RB Parent"].values["Transform"]).position;
            path[0].rot = ((Transform)playerObjects["RB Parent"].values["Transform"]).rotation;
            for (int i = 1; i < path.Count; i++)
            {
                if (Vector3.Distance(path[i].pos, path[i - 1].pos) > tailDistance)
                {
                    Vector3 pos = Vector3.Lerp(path[i].pos, path[i - 1].pos, Time.deltaTime * 12f);
                    Quaternion rot = Quaternion.Lerp(path[i].rot, path[i - 1].rot, Time.deltaTime * 12f);
                    path[i].pos = pos;
                    path[i].rot = rot;
                }
            }
        }

        public void UpdateTailTransform()
        {
            if (tailMode != 0)
                return;
            if (GameManager.inst == null || GameManager.inst.gameState != GameManager.State.Paused)
            {
                float num = Time.deltaTime * 200f;
                for (int i = 1; i < path.Count; i++)
                {
                    if (InputDataManager.inst.players.Count > 0 && path.Count >= i && path[i].transform != null)
                    {
                        num *= Vector3.Distance(path[i].lastPos, path[i].pos);
                        path[i].transform.position = Vector3.MoveTowards(path[i].lastPos, path[i].pos, num);
                        path[i].lastPos = path[i].transform.position;
                        path[i].transform.rotation = path[i].rot;
                    }
                }
            }
        }

        private void Spawn()
        {
            //var anim = (Animator)playerObjects["Base"].values["Animator"];
            var currentModel = PlayerPlugin.playerModels[PlayerPlugin.playerModelsIndex[playerIndex]];
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
        }

        public void OnChildTriggerEnter(Collider2D _other)
        {
            if ((EditorManager.inst != null && PlayerPlugin.zenModeInEditor.Value || EditorManager.inst == null) && _other.tag != "Helper" && _other.tag != "Player" && CanTakeDamage && !isBoosting)
            {
                PlayerHit();
            }
        }

        public void OnChildTriggerEnterMesh(Collider _other)
        {
            if ((EditorManager.inst != null && PlayerPlugin.zenModeInEditor.Value || EditorManager.inst == null) && _other.tag != "Helper" && _other.tag != "Player" && CanTakeDamage && !isBoosting)
            {
                PlayerHit();
            }
        }

        public void OnChildTriggerStay(Collider2D _other)
        {
            if ((EditorManager.inst != null && PlayerPlugin.zenModeInEditor.Value || EditorManager.inst == null) && _other.tag != "Helper" && _other.tag != "Player" && CanTakeDamage)
            {
                PlayerHit();
            }
        }

        public void OnChildTriggerStayMesh(Collider _other)
        {
            if ((EditorManager.inst != null && PlayerPlugin.zenModeInEditor.Value || EditorManager.inst == null) && _other.tag != "Helper" && _other.tag != "Player" && CanTakeDamage)
            {
                PlayerHit();
            }
        }

        private void PlayerHit()
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
            }
        }

        private IEnumerator BoostCooldownLoop()
        {
            yield return new WaitForSeconds(boostCooldown);
            CanBoost = true;
            yield break;
        }

        private IEnumerator Kill()
        {
            isDead = true;
            if (playerDeathEvent != null)
            {
                playerDeathEvent(((Rigidbody2D)playerObjects["RB Parent"].values["Rigidbody2D"]).position);
            }
            InputDataManager.inst.players[playerIndex].active = false;
            InputDataManager.inst.players[playerIndex].health = 0;
            ((Animator)playerObjects["Base"].values["Animator"]).SetTrigger("kill");
            InputDataManager.inst.SetControllerRumble(playerIndex, 1f);
            yield return new WaitForSecondsRealtime(0.2f);
            PlayerPlugin.players.RemoveAt(playerIndex);
            Destroy(gameObject);
            InputDataManager.inst.StopControllerRumble(playerIndex);
            yield break;
        }

        public void InitMidSpawn()
        {
            CanMove = true;
            CanBoost = true;
        }

        public void InitAfterSpawn()
        {
            if (boostCoroutine != null)
            {
                StopCoroutine(boostCoroutine);
            }
            CanMove = true;
            CanBoost = true;
            CanTakeDamage = true;
        }

        private void StartBoost()
        {
            if (CanBoost && !isBoosting)
            {
                var anim = (Animator)playerObjects["Base"].values["Animator"];

                startBoostTime = Time.time;
                InitBeforeBoost();
                anim.SetTrigger("boost");

                var ps = (ParticleSystem)playerObjects["Boost Particles"].values["ParticleSystem"];
                var emission = ps.emission;

                var currentModel = PlayerPlugin.playerModels[PlayerPlugin.playerModelsIndex[playerIndex]];
                var headTrail = (TrailRenderer)playerObjects["Boost Trail"].values["TrailRenderer"];

                if (emission.enabled)
                {
                    ps.Play();
                }
                if ((bool)currentModel.values["Head Trail Emitting"])
                {
                    headTrail.emitting = true;
                }
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

        public IEnumerator DamageSetDelay(float _offset)
        {
            yield return new WaitForSeconds(_offset);
            CanTakeDamage = true;
            yield break;
        }

        public void InitAfterBoost()
        {
            isBoosting = false;
            isBoostCancelled = false;
            boostCoroutine = StartCoroutine(BoostCooldownLoop());

            var currentModel = PlayerPlugin.playerModels[PlayerPlugin.playerModelsIndex[playerIndex]];
            var headTrail = (TrailRenderer)playerObjects["Boost Trail"].values["TrailRenderer"];
            if ((bool)currentModel.values["Head Trail Emitting"])
            {
                headTrail.emitting = false;
            }
        }

        public void InitBeforeHit()
        {
            startHurtTime = Time.time;
            CanBoost = true;
            isBoosting = false;
            isTakingHit = true;
            CanTakeDamage = false;
            AudioManager.inst.PlaySound("HurtPlayer");
        }

        public void InitAfterHit()
        {
            isTakingHit = false;
            CanTakeDamage = true;
        }

        public void ResetMovement()
        {
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
            if (playerObjects["Head"].gameObject.transform.Find("death-explosion").GetComponent<Animator>())
            {
                playerObjects["Head"].gameObject.transform.Find("death-explosion").GetComponent<ParticleSystem>().Play();
            }
        }

        public void PlayHitParticles()
        {
            if (playerObjects["Head"].gameObject.transform.Find("burst-explosion").GetComponent<Animator>())
            {
                playerObjects["Head"].gameObject.transform.Find("burst-explosion").GetComponent<ParticleSystem>().Play();
            }
        }

        public void SetColor(Color _col, Color _colTail)
        {
            if (playerObjects["Head"].values["MeshRenderer"] != null)
            {
                ((MeshRenderer)playerObjects["Head"].values["MeshRenderer"]).material.color = _col;
            }
            //new Color(_col.r, _col.g, _col.b, 1f);
            if (playerObjects["Head"].gameObject.transform.Find("burst-explosion").GetComponent<ParticleSystem>() != null)
            {
                var main = playerObjects["Head"].gameObject.transform.Find("burst-explosion").GetComponent<ParticleSystem>().main;
                main.startColor = new ParticleSystem.MinMaxGradient(_col);
            }
            if (playerObjects["Head"].gameObject.transform.Find("death-explosion").GetComponent<ParticleSystem>() != null)
            {
                var main = playerObjects["Head"].gameObject.transform.Find("death-explosion").GetComponent<ParticleSystem>().main;
                main.startColor = new ParticleSystem.MinMaxGradient(_col);
            }
            if (playerObjects["Head"].gameObject.transform.Find("spawn-implosion").GetComponent<ParticleSystem>() != null)
            {
                var main = playerObjects["Head"].gameObject.transform.Find("spawn-implosion").GetComponent<ParticleSystem>().main;
                main.startColor = new ParticleSystem.MinMaxGradient(_col);
            }
            if (playerObjects["Boost"].values["MeshRenderer"] != null)
            {
                ((MeshRenderer)playerObjects["Boost"].values["MeshRenderer"]).material.color = _colTail;
            }

            var currentModel = PlayerPlugin.playerModels[PlayerPlugin.playerModelsIndex[playerIndex]];
            if (currentModel != null)
            {
                for (int i = 1; i < 4; i++)
                {
                    int colStart = (int)currentModel.values[string.Format("Tail {0} Trail Start Color", i)];
                    float alphaStart = (float)currentModel.values[string.Format("Tail {0} Trail Start Opacity", i)];
                    int colEnd = (int)currentModel.values[string.Format("Tail {0} Trail End Color", i)];
                    float alphaEnd = (float)currentModel.values[string.Format("Tail {0} Trail End Opacity", i)];

                    ((MeshRenderer)playerObjects[string.Format("Tail {0}", i)].values["MeshRenderer"]).material.color = _colTail;

                    var trailRenderer = (TrailRenderer)playerObjects[string.Format("Tail {0}", i)].values["TrailRenderer"];
                    if (colStart < 4)
                    {
                        trailRenderer.startColor = LSColors.fadeColor(GameManager.inst.LiveTheme.playerColors[colStart], alphaStart);
                    }
                    if (colEnd < 4)
                    {
                        trailRenderer.endColor = LSColors.fadeColor(GameManager.inst.LiveTheme.playerColors[colEnd], alphaEnd);
                    }
                    if (colStart == 4)
                    {
                        trailRenderer.startColor = LSColors.fadeColor(GameManager.inst.LiveTheme.guiColor, alphaStart);
                    }
                    if (colEnd == 4)
                    {
                        trailRenderer.endColor = LSColors.fadeColor(GameManager.inst.LiveTheme.guiColor, alphaEnd);
                    }
                    if (colStart > 4)
                    {
                        trailRenderer.startColor = LSColors.fadeColor(GameManager.inst.LiveTheme.objectColors[colStart - 5], alphaStart);
                    }
                    if (colEnd > 4)
                    {
                        trailRenderer.startColor = LSColors.fadeColor(GameManager.inst.LiveTheme.objectColors[colStart - 5], alphaEnd);
                    }
                }

                if (playerObjects["Head Trail"].values["TrailRenderer"] != null && (bool)currentModel.values["Head Trail Emitting"])
                {
                    int colStart = (int)currentModel.values["Head Trail Start Color"];
                    float alphaStart = (float)currentModel.values["Head Trail Start Opacity"];
                    int colEnd = (int)currentModel.values["Head Trail End Color"];
                    float alphaEnd = (float)currentModel.values["Head Trail End Opacity"];

                    if (colStart < 4)
                    {
                        ((TrailRenderer)playerObjects["Head Trail"].values["TrailRenderer"]).startColor = LSColors.fadeColor(GameManager.inst.LiveTheme.playerColors[colStart], alphaStart);
                    }
                    if (colEnd < 4)
                    {
                        ((TrailRenderer)playerObjects["Head Trail"].values["TrailRenderer"]).endColor = LSColors.fadeColor(GameManager.inst.LiveTheme.playerColors[colEnd], alphaEnd);
                    }
                    if (colStart == 4)
                    {
                        ((TrailRenderer)playerObjects["Head Trail"].values["TrailRenderer"]).startColor = LSColors.fadeColor(GameManager.inst.LiveTheme.guiColor, alphaStart);
                    }
                    if (colEnd == 4)
                    {
                        ((TrailRenderer)playerObjects["Head Trail"].values["TrailRenderer"]).endColor = LSColors.fadeColor(GameManager.inst.LiveTheme.guiColor, alphaEnd);
                    }
                    if (colStart > 4)
                    {
                        ((TrailRenderer)playerObjects["Head Trail"].values["TrailRenderer"]).startColor = LSColors.fadeColor(GameManager.inst.LiveTheme.objectColors[colStart - 5], alphaStart);
                    }
                    if (colEnd > 4)
                    {
                        ((TrailRenderer)playerObjects["Head Trail"].values["TrailRenderer"]).endColor = LSColors.fadeColor(GameManager.inst.LiveTheme.objectColors[colEnd - 5], alphaEnd);
                    }
                }
                if (playerObjects["Head Particles"].values["ParticleSystem"] != null && (bool)currentModel.values["Head Particles Emitting"])
                {
                    int colStart = (int)currentModel.values["Head Particles Color"];

                    var ps = (ParticleSystem)playerObjects["Head Particles"].values["ParticleSystem"];
                    var main = ps.main;

                    if (colStart < 4)
                    {
                        main.startColor = GameManager.inst.LiveTheme.playerColors[colStart];
                        //psCol.gradient.colorKeys[0].color = GameManager.inst.LiveTheme.playerColors[colStart];
                    }
                    //if (colEnd < 4)
                    //{
                    //psCol.gradient.colorKeys[1].color = GameManager.inst.LiveTheme.playerColors[colEnd];
                    //}
                    if (colStart == 4)
                    {
                        main.startColor = GameManager.inst.LiveTheme.guiColor;
                        //psCol.gradient.colorKeys[0].color = GameManager.inst.LiveTheme.guiColor;
                    }
                    //if (colEnd == 4)
                    //{
                    //psCol.gradient.colorKeys[1].color = GameManager.inst.LiveTheme.guiColor;
                    //}
                    if (colStart > 4)
                    {
                        main.startColor = GameManager.inst.LiveTheme.objectColors[colStart - 5];
                        //psCol.gradient.colorKeys[0].color = GameManager.inst.LiveTheme.objectColors[colStart - 5];
                    }
                    //if (colEnd > 4)
                    //{
                    //psCol.gradient.colorKeys[1].color = GameManager.inst.LiveTheme.objectColors[colEnd - 5];
                    //}
                }
                if (playerObjects["Boost Trail"].values["TrailRenderer"] != null && (bool)currentModel.values["Boost Trail Emitting"])
                {
                    int colStart = (int)currentModel.values["Boost Trail Start Color"];
                    float alphaStart = (float)currentModel.values["Boost Trail Start Opacity"];
                    int colEnd = (int)currentModel.values["Boost Trail End Color"];
                    float alphaEnd = (float)currentModel.values["Boost Trail End Opacity"];

                    if (colStart < 4)
                    {
                        ((TrailRenderer)playerObjects["Boost Trail"].values["TrailRenderer"]).startColor = LSColors.fadeColor(GameManager.inst.LiveTheme.playerColors[colStart], alphaStart);
                    }
                    if (colEnd < 4)
                    {
                        ((TrailRenderer)playerObjects["Boost Trail"].values["TrailRenderer"]).endColor = LSColors.fadeColor(GameManager.inst.LiveTheme.playerColors[colEnd], alphaEnd);
                    }
                    if (colStart == 4)
                    {
                        ((TrailRenderer)playerObjects["Boost Trail"].values["TrailRenderer"]).startColor = LSColors.fadeColor(GameManager.inst.LiveTheme.guiColor, alphaStart);
                    }
                    if (colEnd == 4)
                    {
                        ((TrailRenderer)playerObjects["Boost Trail"].values["TrailRenderer"]).endColor = LSColors.fadeColor(GameManager.inst.LiveTheme.guiColor, alphaEnd);
                    }
                    if (colStart > 4)
                    {
                        ((TrailRenderer)playerObjects["Boost Trail"].values["TrailRenderer"]).startColor = LSColors.fadeColor(GameManager.inst.LiveTheme.objectColors[colStart - 5], alphaStart);
                    }
                    if (colEnd > 4)
                    {
                        ((TrailRenderer)playerObjects["Boost Trail"].values["TrailRenderer"]).endColor = LSColors.fadeColor(GameManager.inst.LiveTheme.objectColors[colEnd - 5], alphaEnd);
                    }
                }
                if (playerObjects["Boost Particles"].values["ParticleSystem"] != null && (bool)currentModel.values["Boost Particles Emitting"])
                {
                    int colStart = (int)currentModel.values["Boost Particles Color"];

                    var ps = (ParticleSystem)playerObjects["Boost Particles"].values["ParticleSystem"];
                    var main = ps.main;

                    if (colStart < 4)
                    {
                        main.startColor = GameManager.inst.LiveTheme.playerColors[colStart];
                    }
                    if (colStart == 4)
                    {
                        main.startColor = GameManager.inst.LiveTheme.guiColor;
                    }
                    if (colStart > 4)
                    {
                        main.startColor = GameManager.inst.LiveTheme.objectColors[colStart - 5];
                    }
                }
            }
        }

        public void updatePlayer()
        {
            var currentModel = PlayerPlugin.playerModels[PlayerPlugin.playerModelsIndex[playerIndex]];
            var rb = (Rigidbody2D)playerObjects["RB Parent"].values["Rigidbody2D"];

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

                Debug.LogFormat("{0}Rendering Head\nPos: {1}\nSca: {2}\nRot: {3}", PlayerPlugin.className, h1, h2, h3);

                playerObjects["Head"].gameObject.transform.localPosition = new Vector3(h1.x, h1.y, 0f);
                playerObjects["Head"].gameObject.transform.localScale = new Vector3(h2.x, h2.y, 1f);
                playerObjects["Head"].gameObject.transform.localEulerAngles = new Vector3(0f, 0f, h3);

                var b1 = (Vector2)currentModel.values["Boost Position"];
                var b2 = (Vector2)currentModel.values["Boost Scale"];
                var b3 = (float)currentModel.values["Boost Rotation"];

                Debug.LogFormat("{0}Rendering Boost\nPos: {1}\nSca: {2}\nRot: {3}", PlayerPlugin.className, b1, b2, b3);

                ((MeshRenderer)playerObjects["Boost"].values["MeshRenderer"]).enabled = (bool)currentModel.values["Boost Active"];
                playerObjects["Boost Base"].gameObject.transform.localPosition = new Vector3(b1.x, b1.y, 1f);
                playerObjects["Boost Base"].gameObject.transform.localScale = new Vector3(b2.x, b2.y, 1f);
                playerObjects["Boost Base"].gameObject.transform.localEulerAngles = new Vector3(0f, 0f, b3);

                tailDistance = (float)currentModel.values["Tail Base Distance"];
                tailMode = (int)currentModel.values["Tail Base Mode"];

                for (int i = 1; i < 4; i++)
                {

                    var delayTracker = (DelayTracker)playerObjects[string.Format("Tail {0} Base", i)].values["DelayTracker"];
                    delayTracker.offset = -i * tailDistance / 2f;
                    delayTracker.followSharpness = 0.1f * (-i + 4);

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
                        InputDataManager.inst.players[playerIndex].health = 1;
                        UpdateTail(InputDataManager.inst.players[playerIndex].health, rb.position);
                    }
                    else
                    {
                        InputDataManager.inst.players[playerIndex].health = (int)currentModel.values["Base Health"];
                        UpdateTail(InputDataManager.inst.players[playerIndex].health, rb.position);
                    }
                }

                //Trail
                {
                    var headTrail = (TrailRenderer)playerObjects["Head Trail"].values["TrailRenderer"];

                    playerObjects["Head Trail"].gameObject.transform.localPosition = (Vector2)currentModel.values["Head Trail Position Offset"];

                    headTrail.enabled = (bool)currentModel.values["Head Trail Emitting"];
                    headTrail.time = (float)currentModel.values["Head Trail Time"];
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

                    //ssss.curve.keys[0].value = sizeStart;
                    //ssss.curve.keys[1].value = sizeEnd;

                    //ssss.mode = ParticleSystemCurveMode.Constant;
                    //ssss.mode = ParticleSystemCurveMode.Curve;

                    Debug.LogFormat("{0}Key 0 Value: {1}\nSupposed to be: {2}", PlayerPlugin.className, ssss.curve.keys[0].value, sizeStart);
                    Debug.LogFormat("{0}Key 1 Value: {1}\nSupposed to be: {2}", PlayerPlugin.className, ssss.curve.keys[1].value, sizeEnd);

                    sizeOverLifetime.size = ssss;
                }

                //Boost Trail
                {
                    var headTrail = (TrailRenderer)playerObjects["Boost Trail"].values["TrailRenderer"];
                    headTrail.enabled = (bool)currentModel.values["Boost Trail Emitting"];
                    headTrail.emitting = (bool)currentModel.values["Boost Trail Emitting"];
                    headTrail.time = (float)currentModel.values["Boost Trail Time"];
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

                    Debug.LogFormat("{0}Key 0 Value: {1}\nSupposed to be: {2}", PlayerPlugin.className, ssss.curve.keys[0].value, sizeStart);
                    Debug.LogFormat("{0}Key 1 Value: {1}\nSupposed to be: {2}", PlayerPlugin.className, ssss.curve.keys[1].value, sizeEnd);

                    sizeOverLifetime.size = ssss;
                }

                //Tails Trail / Particles
                {
                    for (int i = 1; i < 4; i++)
                    {
                        var headTrail = (TrailRenderer)playerObjects[string.Format("Tail {0}", i)].values["TrailRenderer"];
                        headTrail.enabled = (bool)currentModel.values[string.Format("Tail {0} Trail Emitting", i)];
                        headTrail.emitting = (bool)currentModel.values[string.Format("Tail {0} Trail Emitting", i)];
                        headTrail.time = (float)currentModel.values[string.Format("Tail {0} Trail Time", i)];
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

                        Debug.LogFormat("{0}Key 0 Value: {1}\nSupposed to be: {2}", PlayerPlugin.className, ssss.curve.keys[0].value, sizeStart);
                        Debug.LogFormat("{0}Key 1 Value: {1}\nSupposed to be: {2}", PlayerPlugin.className, ssss.curve.keys[1].value, sizeEnd);

                        sizeOverLifetime.size = ssss;
                    }
                }
            }

            CreateAll();
        }

        public void updateParticlesCurvesBecauseTheySuck()
        {
            var currentModel = PlayerPlugin.playerModels[PlayerPlugin.playerModelsIndex[playerIndex]];
            //Head
            {
                var headParticles = (ParticleSystem)playerObjects["Head Particles"].values["ParticleSystem"];

                var colorOverLifetime = headParticles.colorOverLifetime;
                colorOverLifetime.enabled = true;
                var psCol = colorOverLifetime.color;

                float alphaStart = (float)currentModel.values["Head Particles Start Opacity"];
                float alphaEnd = (float)currentModel.values["Head Particles End Opacity"];

                if (psCol.gradient == null)
                    psCol.gradient = new Gradient();
                if (psCol.gradient.alphaKeys[0].alpha != alphaStart)
                {
                    psCol.gradient.alphaKeys[0].alpha = alphaStart;
                    psCol.mode = ParticleSystemGradientMode.Color;
                    psCol.mode = ParticleSystemGradientMode.Gradient;
                }
                if (psCol.gradient.alphaKeys[1].alpha != alphaEnd)
                {
                    psCol.gradient.alphaKeys[1].alpha = alphaEnd;
                    psCol.mode = ParticleSystemGradientMode.Color;
                    psCol.mode = ParticleSystemGradientMode.Gradient;
                }

                var sizeOverLifetime = headParticles.sizeOverLifetime;
                sizeOverLifetime.enabled = true;

                var ssss = sizeOverLifetime.size;

                var sizeStart = (float)currentModel.values["Head Particles Start Scale"];
                var sizeEnd = (float)currentModel.values["Head Particles End Scale"];

                if (ssss.curve == null)
                    ssss.curve = new AnimationCurve();
                if (ssss.curve.keys[0].value != sizeStart)
                {
                    ssss.curve.keys[0].value = sizeStart;
                    ssss.mode = ParticleSystemCurveMode.Constant;
                    ssss.mode = ParticleSystemCurveMode.Curve;
                }
                if (ssss.curve.keys[1].value != sizeEnd)
                {
                    ssss.curve.keys[1].value = sizeEnd;
                    ssss.mode = ParticleSystemCurveMode.Constant;
                    ssss.mode = ParticleSystemCurveMode.Curve;
                }
            }

            //Boost
            {
                var headParticles = (ParticleSystem)playerObjects["Boost Particles"].values["ParticleSystem"];

                var colorOverLifetime = headParticles.colorOverLifetime;
                colorOverLifetime.enabled = true;
                var psCol = colorOverLifetime.color;

                float alphaStart = (float)currentModel.values["Boost Particles Start Opacity"];
                float alphaEnd = (float)currentModel.values["Boost Particles End Opacity"];

                if (psCol.gradient == null)
                    psCol.gradient = new Gradient();
                if (psCol.gradient.alphaKeys[0].alpha != alphaStart)
                {
                    psCol.gradient.alphaKeys[0].alpha = alphaStart;
                    psCol.mode = ParticleSystemGradientMode.Color;
                    psCol.mode = ParticleSystemGradientMode.Gradient;
                }
                if (psCol.gradient.alphaKeys[1].alpha != alphaEnd)
                {
                    psCol.gradient.alphaKeys[1].alpha = alphaEnd;
                    psCol.mode = ParticleSystemGradientMode.Color;
                    psCol.mode = ParticleSystemGradientMode.Gradient;
                }

                var sizeOverLifetime = headParticles.sizeOverLifetime;
                sizeOverLifetime.enabled = true;

                var ssss = sizeOverLifetime.size;

                var sizeStart = (float)currentModel.values["Boost Particles Start Scale"];
                var sizeEnd = (float)currentModel.values["Boost Particles End Scale"];

                if (ssss.curve == null)
                    ssss.curve = new AnimationCurve();
                if (ssss.curve.keys[0].value != sizeStart)
                {
                    ssss.curve.keys[0].value = sizeStart;
                    ssss.mode = ParticleSystemCurveMode.Constant;
                    ssss.mode = ParticleSystemCurveMode.Curve;
                }
                if (ssss.curve.keys[0].value != sizeEnd)
                {
                    ssss.curve.keys[0].value = sizeEnd;
                    ssss.mode = ParticleSystemCurveMode.Constant;
                    ssss.mode = ParticleSystemCurveMode.Curve;
                }
            }

            //Tails
            {
                for (int i = 1; i < 4; i++)
                {
                    var headParticles = (ParticleSystem)playerObjects[string.Format("Tail {0} Particles", i)].values["ParticleSystem"];

                    var colorOverLifetime = headParticles.colorOverLifetime;
                    colorOverLifetime.enabled = true;
                    var psCol = colorOverLifetime.color;

                    float alphaStart = (float)currentModel.values[string.Format("Tail {0} Particles Start Opacity", i)];
                    float alphaEnd = (float)currentModel.values[string.Format("Tail {0} Particles End Opacity", i)];

                    if (psCol.gradient == null)
                        psCol.gradient = new Gradient();
                    if (psCol.gradient.alphaKeys[0].alpha != alphaStart)
                    {
                        psCol.gradient.alphaKeys[0].alpha = alphaStart;
                        psCol.mode = ParticleSystemGradientMode.Color;
                        psCol.mode = ParticleSystemGradientMode.Gradient;
                    }
                    if (psCol.gradient.alphaKeys[1].alpha != alphaEnd)
                    {
                        psCol.gradient.alphaKeys[1].alpha = alphaEnd;
                        psCol.mode = ParticleSystemGradientMode.Color;
                        psCol.mode = ParticleSystemGradientMode.Gradient;
                    }

                    var sizeOverLifetime = headParticles.sizeOverLifetime;
                    sizeOverLifetime.enabled = true;

                    var ssss = sizeOverLifetime.size;

                    var sizeStart = (float)currentModel.values[string.Format("Tail {0} Particles Start Scale", i)];
                    var sizeEnd = (float)currentModel.values[string.Format("Tail {0} Particles End Scale", i)];

                    if (ssss.curve == null)
                        ssss.curve = new AnimationCurve();
                    if (ssss.curve.keys[0].value != sizeStart)
                    {
                        ssss.curve.keys[0].value = sizeStart;
                        ssss.mode = ParticleSystemCurveMode.Constant;
                        ssss.mode = ParticleSystemCurveMode.Curve;
                    }
                    if (ssss.curve.keys[1].value != sizeEnd)
                    {
                        ssss.curve.keys[1].value = sizeEnd;
                        ssss.mode = ParticleSystemCurveMode.Constant;
                        ssss.mode = ParticleSystemCurveMode.Curve;
                    }
                }
            }
        }

        public void updateCustomObjects(string id = "")
        {
            if (customObjects.Count > 0)
            {
                foreach (var obj in customObjects)
                {
                    if (obj.Value.gameObject != null && id == "")
                        Destroy(obj.Value.gameObject);

                    if (obj.Value.gameObject != null && id != "" && obj.Key == id)
                    {
                        Destroy(obj.Value.gameObject);
                    }
                }

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

                            switch ((int)customObj.values["Parent"])
                            {
                                case 0:
                                    {
                                        customObj.gameObject.transform.SetParent(playerObjects["RB Parent"].gameObject.transform);
                                        break;
                                    }
                                case 1:
                                    {
                                        customObj.gameObject.transform.SetParent(playerObjects["Boost Base"].gameObject.transform);
                                        break;
                                    }
                                case 2:
                                    {
                                        customObj.gameObject.transform.SetParent(playerObjects["Tail 1 Base"].gameObject.transform);
                                        break;
                                    }
                                case 3:
                                    {
                                        customObj.gameObject.transform.SetParent(playerObjects["Tail 2 Base"].gameObject.transform);
                                        break;
                                    }
                                case 4:
                                    {
                                        customObj.gameObject.transform.SetParent(playerObjects["Tail 3 Base"].gameObject.transform);
                                        break;
                                    }
                            }

                            customObj.gameObject.transform.localPosition = new Vector3(pos.x, pos.y, depth);
                            customObj.gameObject.transform.localScale = new Vector3(sca.x, sca.y, 1f);
                            customObj.gameObject.transform.localEulerAngles = new Vector3(0f, 0f, rot);

                            customObj.gameObject.tag = "Helper";
                            customObj.gameObject.transform.GetChild(0).tag = "Helper";

                            customObj.values["MeshRenderer"] = customObj.gameObject.GetComponentInChildren<MeshRenderer>();
                        }
                    }
                }
            }
        }

        public void CreateAll(bool _update = true)
        {
            var currentModel = PlayerPlugin.playerModels[PlayerPlugin.playerModelsIndex[playerIndex]];

            Dictionary<string, object> dictionary = (Dictionary<string, object>)currentModel.values["Custom Objects"];
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
                    c.values["Opacity"] = customObj["Opacity"];
                    c.values["Parent"] = customObj["Parent"];
                    c.values["Depth"] = customObj["Depth"];

                    customObjects.Add((string)customObj["ID"], c);
                }

            if (_update)
                updateCustomObjects();
        }

        public PlayerObject CreateCustomObject()
        {
            var obj = new PlayerObject();

            Debug.LogFormat("{0}Creating new PlayerObject!", PlayerPlugin.className);

            obj.name = "Object";
            obj.values = new Dictionary<string, object>();
            obj.values.Add("Shape", new Vector2Int(0, 0));
            obj.values.Add("Position", new Vector2(0f, 0f));
            obj.values.Add("Scale", new Vector2(1f, 1f));
            obj.values.Add("Rotation", 0f);
            obj.values.Add("Color", 0);
            obj.values.Add("Opacity", 0f);
            obj.values.Add("Parent", 0);
            obj.values.Add("Depth", 0f);
            obj.values.Add("MeshRenderer", null);
            var id = LSText.randomNumString(16);
            obj.values.Add("ID", id);

            Debug.LogFormat("{0}Created new PlayerObject with id {1}!", PlayerPlugin.className, id);

            return obj;
        }

        public void CreateBoostEffect()
        {
            var gm = Instantiate(ObjectManager.inst.objectPrefabs[0].options[0]);

            var obj = new PlayerObject();

            gm.transform.DOScale(2f, 1f).SetEase(DataManager.inst.AnimationList[2].Animation);
        }

        public void UpdateCustomTheme()
        {
            if (customObjects.Count > 0)
                foreach (var obj in customObjects.Values)
                {
                    int col = (int)obj.values["Color"];
                    float alpha = (float)obj.values["Opacity"];
                    if (((MeshRenderer)obj.values["MeshRenderer"]) != null)
                    {
                        if (col < 4)
                            ((MeshRenderer)obj.values["MeshRenderer"]).material.color = LSColors.fadeColor(GameManager.inst.LiveTheme.playerColors[col], alpha);
                        if (col == 4)
                            ((MeshRenderer)obj.values["MeshRenderer"]).material.color = LSColors.fadeColor(GameManager.inst.LiveTheme.guiColor, alpha);
                        if (col > 4)
                            ((MeshRenderer)obj.values["MeshRenderer"]).material.color = LSColors.fadeColor(GameManager.inst.LiveTheme.objectColors[col - 5], alpha);
                    }
                }
        }

        public void UpdateTail(int _health, Vector3 _pos)
        {
            Debug.LogFormat("{0}Update Tail: Player: [{1}] - Health: [{2}]", PlayerPlugin.className, playerIndex, _health);
            for (int i = 1; i < path.Count; i++)
            {
                if (path[i].transform != null)
                {
                    if (i > _health)
                        path[i].transform.gameObject.SetActive(false);
                    else
                        path[i].transform.gameObject.SetActive(true);
                }
            }
            //for (int j = 1; j < _health + 1; j++)
            //{
            //    if (path.Count > _health + 1 && path[j].transform != null)
            //        path[j].transform.gameObject.SetActive(true);
            //}
        }

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

            public Vector3 lastPos;
            public Vector3 pos;

            public Quaternion rot;

            public Transform transform;
        }
    }
}
