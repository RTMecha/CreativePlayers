using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

using DG.Tweening;

namespace CreativePlayers.Functions.Components
{
    public class PlayerBoostHandler : MonoBehaviour
    {
        public float minBoostTime = 0.07f;
        public static bool playerCanStretch = false;
        public static StretchDirection playerStretchDirection = StretchDirection.X;
        public static SquashEasing playerSquashEasing = SquashEasing.Elastic;
        public static float playerStretchAmount = 1f;
        public static float playerStretchSpeed = 1f;

        private bool stoppedMoving = true;
        public bool wasBoosting;
        public bool canBoost;
        public Vector2 velocity;
        public StretchDirection stretchDirection;
        public SquashEasing squashEasing;
        public enum StretchDirection
        {
            X,
            Y
        }
        public enum SquashEasing
        {
            Back,
            Elastic,
            Bounce,
            Sine,
            Circ,
            Expo
        }

        private void Update()
        {
            if (!wasBoosting && canBoost && playerCanStretch)
            {
                var currentModel = PlayerPlugin.playerModels.ElementAt(PlayerPlugin.currentModelIndex).Value;
                float x = velocity.x * 0.05f;
                float y = velocity.y * 0.05f;

                if (x < 0f)
                {
                    x = -x;
                }
                if (y < 0f)
                {
                    y = -y;
                }

                //X should always be RIGHT
                //Y should always by UP

                Vector3 v = new Vector3(x, y, 0f);
                if (v.magnitude > 1f)
                {
                    v = v.normalized;
                }

                float sca = v.x + v.y;

                if (v != Vector3.zero)
                {
                    stoppedMoving = false;
                    if (stretchDirection == StretchDirection.X)
                    {
                        transform.localScale = new Vector3(((Vector2)currentModel.values["Head Scale"]).x + sca * 0.5f * playerStretchAmount, ((Vector2)currentModel.values["Head Scale"]).y - sca * 0.2f * playerStretchAmount, 1f);
                    }
                    if (stretchDirection == StretchDirection.Y)
                    {
                        transform.localScale = new Vector3(((Vector2)currentModel.values["Head Scale"]).x - sca * 0.2f * playerStretchAmount, ((Vector2)currentModel.values["Head Scale"]).y + sca * 0.5f * playerStretchAmount, 1f);
                    }
                }
                else if (!stoppedMoving)
                {
                    stoppedMoving = true;
                    transform.DOKill(false);
                    if (squashEasing == SquashEasing.Elastic)
                    {
                        transform.DOLocalMove(new Vector3(((Vector2)currentModel.values["Head Position"]).x, ((Vector2)currentModel.values["Head Position"]).y, 0f), 0.7f).SetEase(DataManager.inst.AnimationList[6].Animation).Play();
                        transform.DOScale(new Vector3(((Vector2)currentModel.values["Head Scale"]).x, ((Vector2)currentModel.values["Head Scale"]).y, 0.7f), playerStretchAmount).SetEase(DataManager.inst.AnimationList[6].Animation).Play();
                    }
                    if (squashEasing == SquashEasing.Back)
                    {
                        transform.DOLocalMove(new Vector3(((Vector2)currentModel.values["Head Position"]).x, ((Vector2)currentModel.values["Head Position"]).y, 0f), 0.7f).SetEase(DataManager.inst.AnimationList[9].Animation).Play();
                        transform.DOScale(new Vector3(((Vector2)currentModel.values["Head Scale"]).x, ((Vector2)currentModel.values["Head Scale"]).y, 1f), 0.7f).SetEase(DataManager.inst.AnimationList[9].Animation).Play();
                    }
                    if (squashEasing == SquashEasing.Bounce)
                    {
                        transform.DOLocalMove(new Vector3(((Vector2)currentModel.values["Head Position"]).x, ((Vector2)currentModel.values["Head Position"]).y, 0f), 0.7f).SetEase(DataManager.inst.AnimationList[12].Animation).Play();
                        transform.DOScale(new Vector3(((Vector2)currentModel.values["Head Scale"]).x, ((Vector2)currentModel.values["Head Scale"]).y, 1f), 0.7f).SetEase(DataManager.inst.AnimationList[12].Animation).Play();
                    }
                }
            }
        }

        public void StartBoosting()
        {
            if (canBoost)
            {
                wasBoosting = true;
                //Debug.Log("Stretch (KILL)");
                transform.DOKill();
                //Debug.Log("Stretch (PLAY)");

                if (stopBoostCoroutine != null)
                {
                    StopCoroutine(stopBoostCoroutine);
                }

                var currentModel = PlayerPlugin.playerModels.ElementAt(PlayerPlugin.currentModelIndex).Value;
                if (stretchDirection == StretchDirection.X)
                {
                    transform.DOLocalMove(new Vector3(((Vector2)currentModel.values["Head Position"]).x * 2f * playerStretchAmount * AudioManager.inst.CurrentAudioSource.pitch, ((Vector2)currentModel.values["Head Position"]).y, 0f), minBoostTime).SetEase(DataManager.inst.AnimationList[2].Animation).Play();
                    transform.DOScale(new Vector3(((Vector2)currentModel.values["Head Scale"]).x * 3f * playerStretchAmount * AudioManager.inst.CurrentAudioSource.pitch, ((Vector2)currentModel.values["Head Scale"]).y * 0.5f * playerStretchSpeed * AudioManager.inst.CurrentAudioSource.pitch, 1f), minBoostTime).SetEase(DataManager.inst.AnimationList[2].Animation).Play();
                }
                if (stretchDirection == StretchDirection.Y)
                {
                    transform.DOLocalMove(new Vector3(((Vector2)currentModel.values["Head Position"]).x, ((Vector2)currentModel.values["Head Position"]).y * 2f * playerStretchAmount * AudioManager.inst.CurrentAudioSource.pitch, 0f), minBoostTime).SetEase(DataManager.inst.AnimationList[2].Animation).Play();
                    transform.DOScale(new Vector3(((Vector2)currentModel.values["Head Scale"]).x * 0.5f * playerStretchAmount * AudioManager.inst.CurrentAudioSource.pitch, ((Vector2)currentModel.values["Head Scale"]).y * 3f * playerStretchSpeed * AudioManager.inst.CurrentAudioSource.pitch, 1f), minBoostTime).SetEase(DataManager.inst.AnimationList[2].Animation).Play();
                }
            }
        }

        public void Stop(float _time)
        {
            stopBoostCoroutine = StartCoroutine(StopBoosting(_time));
        }

        public Coroutine stopBoostCoroutine;

        public IEnumerator StopBoosting(float _time)
        {
            var currentModel = PlayerPlugin.playerModels.ElementAt(PlayerPlugin.currentModelIndex).Value;
            transform.DOKill(false);
            if (squashEasing == SquashEasing.Elastic)
            {
                transform.DOLocalMove(new Vector3(((Vector2)currentModel.values["Head Position"]).x, ((Vector2)currentModel.values["Head Position"]).y, 0f), _time * 0.3f).SetEase(DataManager.inst.AnimationList[6].Animation).Play();
                transform.DOScale(new Vector3(((Vector2)currentModel.values["Head Scale"]).x, ((Vector2)currentModel.values["Head Scale"]).y, _time * 0.3f), playerStretchAmount).SetEase(DataManager.inst.AnimationList[6].Animation).Play();
            }
            if (squashEasing == SquashEasing.Back)
            {
                transform.DOLocalMove(new Vector3(((Vector2)currentModel.values["Head Position"]).x, ((Vector2)currentModel.values["Head Position"]).y, 0f), _time).SetEase(DataManager.inst.AnimationList[9].Animation).Play();
                transform.DOScale(new Vector3(((Vector2)currentModel.values["Head Scale"]).x, ((Vector2)currentModel.values["Head Scale"]).y, 1f), _time).SetEase(DataManager.inst.AnimationList[9].Animation).Play();
            }
            if (squashEasing == SquashEasing.Bounce)
            {
                transform.DOLocalMove(new Vector3(((Vector2)currentModel.values["Head Position"]).x, ((Vector2)currentModel.values["Head Position"]).y, 0f), _time * 0.6f).SetEase(DataManager.inst.AnimationList[12].Animation).Play();
                transform.DOScale(new Vector3(((Vector2)currentModel.values["Head Scale"]).x, ((Vector2)currentModel.values["Head Scale"]).y, 1f), _time * 0.6f).SetEase(DataManager.inst.AnimationList[12].Animation).Play();
            }
            if (squashEasing == SquashEasing.Sine)
            {
                transform.DOLocalMove(new Vector3(((Vector2)currentModel.values["Head Position"]).x, ((Vector2)currentModel.values["Head Position"]).y, 0f), _time * 0.6f).SetEase(DataManager.inst.AnimationList[3].Animation).Play();
                transform.DOScale(new Vector3(((Vector2)currentModel.values["Head Scale"]).x, ((Vector2)currentModel.values["Head Scale"]).y, 1f), _time * 0.6f).SetEase(DataManager.inst.AnimationList[3].Animation).Play();
            }
            if (squashEasing == SquashEasing.Circ)
            {
                transform.DOLocalMove(new Vector3(((Vector2)currentModel.values["Head Position"]).x, ((Vector2)currentModel.values["Head Position"]).y, 0f), _time * 0.6f).SetEase(DataManager.inst.AnimationList[17].Animation).Play();
                transform.DOScale(new Vector3(((Vector2)currentModel.values["Head Scale"]).x, ((Vector2)currentModel.values["Head Scale"]).y, 1f), _time * 0.6f).SetEase(DataManager.inst.AnimationList[17].Animation).Play();
            }
            if (squashEasing == SquashEasing.Expo)
            {
                transform.DOLocalMove(new Vector3(((Vector2)currentModel.values["Head Position"]).x, ((Vector2)currentModel.values["Head Position"]).y, 0f), _time * 0.6f).SetEase(DataManager.inst.AnimationList[21].Animation).Play();
                transform.DOScale(new Vector3(((Vector2)currentModel.values["Head Scale"]).x, ((Vector2)currentModel.values["Head Scale"]).y, 1f), _time * 0.6f).SetEase(DataManager.inst.AnimationList[21].Animation).Play();
            }
            yield return new WaitForSeconds(_time);
            wasBoosting = false;
            yield break;
        }
    }
}
