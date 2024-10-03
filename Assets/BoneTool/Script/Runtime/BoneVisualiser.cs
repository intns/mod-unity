using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

#endif

namespace BoneTool.Script.Runtime
{
    [ExecuteInEditMode]
    public class BoneVisualiser : MonoBehaviour
    {
        public Transform RootNode;
        public float BoneGizmosSize = 0.01f;
        public Color BoneColor = Color.white;
        public bool HideRoot;
        public bool EnableConstraint = true;

        private Transform _preRootNode;
        private Transform[] _childNodes;
        private BoneTransform[] _previousTransforms;

        public Transform[] GetChildNodes()
        {
            return _childNodes;
        }

        private void Update()
        {
            if (EnableConstraint && _previousTransforms != null)
            {
                foreach (BoneTransform boneTransform in _previousTransforms)
                {
                    if (boneTransform.Target && boneTransform.Target.hasChanged)
                    {
                        if (boneTransform.Target.parent.childCount == 1)
                        {
                            boneTransform.Target.localPosition = boneTransform.LocalPosition;
                        }
                        else
                        {
                            boneTransform.SetLocalPosition(boneTransform.Target.localPosition);
                        }
                    }
                }
            }
        }

#if UNITY_EDITOR

        private void OnScene(SceneView sceneview)
        {
            bool shouldDraw = true;

            //Checking if the current game object is inside of a prefab stage
            if (UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null)
                shouldDraw =
                    UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(gameObject)
                    == UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();

            if (RootNode != null && shouldDraw)
            {
                if (
                    _childNodes == null
                    || _childNodes.Length == 0
                    || _previousTransforms == null
                    || _previousTransforms.Length == 0
                )
                    PopulateChildren();

                Handles.color = BoneColor;

                foreach (Transform node in _childNodes)
                {
                    if (!node.transform.parent)
                        continue;
                    if (HideRoot && node == _preRootNode)
                        continue;

                    Vector3 start = node.transform.parent.position;
                    Vector3 end = node.transform.position;

                    if (
                        Handles.Button(
                            node.transform.position,
                            Quaternion.identity,
                            BoneGizmosSize,
                            BoneGizmosSize,
                            Handles.SphereHandleCap
                        )
                    )
                    {
                        Selection.activeGameObject = node.gameObject;
                    }

                    if (HideRoot && node.parent == _preRootNode)
                        continue;

                    Handles.DrawDottedLine(start, end, 0.5f);
                }
            }
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnScene;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnScene;
        }
#endif

        public void PopulateChildren()
        {
            if (!RootNode)
                return;
            _preRootNode = RootNode;
            _childNodes = RootNode.GetComponentsInChildren<Transform>();
            _previousTransforms = new BoneTransform[_childNodes.Length];
            for (int i = 0; i < _childNodes.Length; i++)
            {
                Transform childNode = _childNodes[i];
                _previousTransforms[i] = new BoneTransform(childNode, childNode.localPosition);
            }
        }

        [Serializable]
        private struct BoneTransform
        {
            public Transform Target;
            public Vector3 LocalPosition;

            public BoneTransform(Transform target, Vector3 localPosition)
            {
                Target = target;
                LocalPosition = localPosition;
            }

            public void SetLocalPosition(Vector3 position)
            {
                LocalPosition = position;
            }
        }
    }
}
