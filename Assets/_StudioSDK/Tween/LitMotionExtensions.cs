using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.UI;
namespace LogosSDK.Tween
{

public static class LitMotionExtensions {
    #region Camera
    public static MotionHandle DOFieldOfView(this Camera camera, float from, float to, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            from += camera.fieldOfView;
            to += camera.fieldOfView;
        }
        return LMotion.Create(from, to, duration).WithEase(ease).WithCancelOnError().BindToFieldOfView(camera);
    }

    public static MotionHandle DOFieldOfView(this Camera camera, float value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = camera.fieldOfView;
        var end = relative ? start + value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToFieldOfView(camera);
    }

    public static MotionHandle DOOrthographicSize(this Camera camera, float start, float end, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            start += camera.orthographicSize;
            end += camera.orthographicSize;
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToOrthographicSize(camera);
    }

    public static MotionHandle DOOrthographicSize(this Camera camera, float value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = camera.orthographicSize;
        var end = relative ? start + value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToOrthographicSize(camera);
    }
    #endregion

    #region UI
    public static MotionHandle DOFade(this Graphic target, float start, float end, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            start += target.color.a;
            end += target.color.a;
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToColorA(target);
    }

    public static MotionHandle DOFade(this Graphic target, float value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = target.color.a;
        var end = relative ? start + value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToColorA(target);
    }

    public static MotionHandle DOColor(this Graphic target, Color start, Color end, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            start += target.color;
            end += target.color;
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToColor(target);
    }

    public static MotionHandle DOColor(this Graphic target, Color value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = target.color;
        var end = relative ? start + value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToColor(target);
    }

    public static MotionHandle DOFade(this CanvasGroup canvasGroup, float start, float end, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            start += canvasGroup.alpha;
            end += canvasGroup.alpha;
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToAlpha(canvasGroup);
    }

    public static MotionHandle DOFade(this CanvasGroup canvasGroup, float value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = canvasGroup.alpha;
        var end = relative ? start + value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToAlpha(canvasGroup);
    }

    public static MotionHandle DOFillAmount(this Image image, float start, float end, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            start += image.fillAmount;
            end += image.fillAmount;
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToFillAmount(image);
    }

    public static MotionHandle DOFillAmount(this Image image, float value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = image.fillAmount;
        var end = relative ? start + value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToFillAmount(image);
    }
    #endregion

    #region SpriteRenderer
    public static MotionHandle DOColor(this SpriteRenderer target, Color start, Color end, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            start += target.color;
            end += target.color;
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToColor(target);
    }

    public static MotionHandle DOColor(this SpriteRenderer target, Color value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = target.color;
        var end = relative ? start + value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToColor(target);
    }

    public static MotionHandle DOFade(this SpriteRenderer target, float start, float end, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            start += target.color.a;
            end += target.color.a;
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToColorA(target);
    }

    public static MotionHandle DOFade(this SpriteRenderer target, float value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = target.color.a;
        var end = relative ? start + value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToColorA(target);
    }
    #endregion

    #region Material
    public static MotionHandle DOFloat(this Material material, int propertyId, float start, float end, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            start += material.GetFloat(propertyId);
            end += material.GetFloat(propertyId);
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToMaterialFloat(material, propertyId);
    }

    public static MotionHandle DOFloat(this Material material, int propertyId, float value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = material.GetFloat(propertyId);
        var end = relative ? start + value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToMaterialFloat(material, propertyId);
    }

    public static MotionHandle DOColor(this Material material, int propertyId, Color start, Color end, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            start += material.GetColor(propertyId);
            end += material.GetColor(propertyId);
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToMaterialColor(material, propertyId);
    }

    public static MotionHandle DOColor(this Material material, int propertyId, Color value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = material.GetColor(propertyId);
        var end = relative ? start + value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToMaterialColor(material, propertyId);
    }
    #endregion

    #region RectTransform
    public static MotionHandle DOAnchorPos(this RectTransform rectTransform, Vector2 start, Vector2 end, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            start += rectTransform.anchoredPosition;
            end += rectTransform.anchoredPosition;
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToAnchoredPosition(rectTransform);
    }

    public static MotionHandle DOAnchorPos(this RectTransform rectTransform, Vector2 value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = rectTransform.anchoredPosition;
        var end = relative ? start + value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToAnchoredPosition(rectTransform);
    }

    public static MotionHandle DOAnchorPosX(this RectTransform rectTransform, float start, float end, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            start += rectTransform.anchoredPosition.x;
            end += rectTransform.anchoredPosition.x;
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToAnchoredPositionX(rectTransform);
    }

    public static MotionHandle DOAnchorPosX(this RectTransform rectTransform, float value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = rectTransform.anchoredPosition.x;
        var end = relative ? start + value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToAnchoredPositionX(rectTransform);
    }

    public static MotionHandle DOAnchorPosY(this RectTransform rectTransform, float start, float end, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            start += rectTransform.anchoredPosition.y;
            end += rectTransform.anchoredPosition.y;
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToAnchoredPositionY(rectTransform);
    }

    public static MotionHandle DOAnchorPosY(this RectTransform rectTransform, float value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = rectTransform.anchoredPosition.y;
        var end = relative ? start + value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToAnchoredPositionY(rectTransform);
    }

    public static MotionHandle DOAnchorMin(this RectTransform rectTransform, Vector2 start, Vector2 end, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            start += rectTransform.anchorMin;
            end += rectTransform.anchorMin;
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToAnchorMin(rectTransform);
    }

    public static MotionHandle DOAnchorMin(this RectTransform rectTransform, Vector2 value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = rectTransform.anchorMin;
        var end = relative ? start + value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToAnchorMin(rectTransform);
    }

    public static MotionHandle DOAnchorMax(this RectTransform rectTransform, Vector2 start, Vector2 end, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            start += rectTransform.anchorMax;
            end += rectTransform.anchorMax;
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToAnchorMax(rectTransform);
    }

    public static MotionHandle DOAnchorMax(this RectTransform rectTransform, Vector2 value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = rectTransform.anchorMax;
        var end = relative ? start + value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToAnchorMax(rectTransform);
    }

    public static MotionHandle DOSizeDelta(this RectTransform rectTransform, Vector2 start, Vector2 end, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            start += rectTransform.sizeDelta;
            end += rectTransform.sizeDelta;
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToSizeDelta(rectTransform);
    }

    public static MotionHandle DOSizeDelta(this RectTransform rectTransform, Vector2 value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = rectTransform.sizeDelta;
        var end = relative ? start + value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToSizeDelta(rectTransform);
    }

    public static MotionHandle DOSizeDeltaX(this RectTransform rectTransform, float start, float end, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            start += rectTransform.sizeDelta.x;
            end += rectTransform.sizeDelta.x;
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToSizeDeltaX(rectTransform);
    }

    public static MotionHandle DOSizeDeltaX(this RectTransform rectTransform, float value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = rectTransform.sizeDelta.x;
        var end = relative ? start + value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToSizeDeltaX(rectTransform);
    }

    public static MotionHandle DOSizeDeltaY(this RectTransform rectTransform, float start, float end, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            start += rectTransform.sizeDelta.y;
            end += rectTransform.sizeDelta.y;
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToSizeDeltaY(rectTransform);
    }

    public static MotionHandle DOSizeDeltaY(this RectTransform rectTransform, float value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = rectTransform.sizeDelta.y;
        var end = relative ? start + value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToSizeDeltaY(rectTransform);
    }

    public static MotionHandle DOPivot(this RectTransform rectTransform, Vector2 start, Vector2 end, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            start += rectTransform.pivot;
            end += rectTransform.pivot;
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToPivot(rectTransform);
    }

    public static MotionHandle DOPivot(this RectTransform rectTransform, Vector2 value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = rectTransform.pivot;
        var end = relative ? start + value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToPivot(rectTransform);
    }

    public static MotionHandle DOPivotX(this RectTransform rectTransform, float start, float end, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            start += rectTransform.pivot.x;
            end += rectTransform.pivot.x;
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToPivotX(rectTransform);
    }

    public static MotionHandle DOPivotX(this RectTransform rectTransform, float value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = rectTransform.pivot.x;
        var end = relative ? start + value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToPivotX(rectTransform);
    }

    public static MotionHandle DOPivotY(this RectTransform rectTransform, float start, float end, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            start += rectTransform.pivot.y;
            end += rectTransform.pivot.y;
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToPivotY(rectTransform);
    }

    public static MotionHandle DOPivotY(this RectTransform rectTransform, float value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = rectTransform.pivot.y;
        var end = relative ? start + value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToPivotY(rectTransform);
    }
    #endregion

    #region Transform
    public static MotionHandle DOMove(this Transform transform, Vector3 start, Vector3 end, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            start += transform.position;
            end += transform.position;
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToPosition(transform);
    }

    public static MotionHandle DOMove(this Transform transform, Vector3 value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = transform.position;
        var end = relative ? start + value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToPosition(transform);
    }

    public static MotionHandle DOMoveX(this Transform transform, float start, float end, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            start += transform.position.x;
            end += transform.position.x;
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToPositionX(transform);
    }

    public static MotionHandle DOMoveX(this Transform transform, float value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = transform.position.x;
        var end = relative ? start + value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToPositionX(transform);
    }

    public static MotionHandle DOMoveY(this Transform transform, float start, float end, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            start += transform.position.y;
            end += transform.position.y;
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToPositionY(transform);
    }

    public static MotionHandle DOMoveY(this Transform transform, float value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = transform.position.y;
        var end = relative ? start + value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToPositionY(transform);
    }

    public static MotionHandle DOMoveZ(this Transform transform, float start, float end, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            start += transform.position.z;
            end += transform.position.z;
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToPositionZ(transform);
    }

    public static MotionHandle DOMoveZ(this Transform transform, float value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = transform.position.z;
        var end = relative ? start + value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToPositionZ(transform);
    }

    public static MotionHandle DOLocalMove(this Transform transform, Vector3 start, Vector3 end, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            start += transform.localPosition;
            end += transform.localPosition;
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToLocalPosition(transform);
    }

    public static MotionHandle DOLocalMove(this Transform transform, Vector3 value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = transform.localPosition;
        var end = relative ? start + value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToLocalPosition(transform);
    }

    public static MotionHandle DOLocalMoveX(this Transform transform, float start, float end, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            start += transform.localPosition.x;
            end += transform.localPosition.x;
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToLocalPositionX(transform);
    }

    public static MotionHandle DOLocalMoveX(this Transform transform, float value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = transform.localPosition.x;
        var end = relative ? start + value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToLocalPositionX(transform);
    }

    public static MotionHandle DOLocalMoveY(this Transform transform, float start, float end, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            start += transform.localPosition.y;
            end += transform.localPosition.y;
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToLocalPositionY(transform);
    }

    public static MotionHandle DOLocalMoveY(this Transform transform, float value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = transform.localPosition.y;
        var end = relative ? start + value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToLocalPositionY(transform);
    }

    public static MotionHandle DOLocalMoveZ(this Transform transform, float start, float end, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            start += transform.localPosition.z;
            end += transform.localPosition.z;
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToLocalPositionZ(transform);
    }

    public static MotionHandle DOLocalMoveZ(this Transform transform, float value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = transform.localPosition.z;
        var end = relative ? start + value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToLocalPositionZ(transform);
    }

    public static MotionHandle DORotateQuaternion(this Transform transform, Quaternion start, Quaternion end, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            start = transform.rotation * start;
            end = transform.rotation * end;
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToRotation(transform);
    }

    public static MotionHandle DORotateQuaternion(this Transform transform, Quaternion value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = transform.rotation;
        var end = relative ? start * value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToRotation(transform);
    }

    public static MotionHandle DOLocalRotateQuaternion(this Transform transform, Quaternion start, Quaternion end, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            start = transform.localRotation * start;
            end = transform.localRotation * end;
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToLocalRotation(transform);
    }

    public static MotionHandle DOLocalRotateQuaternion(this Transform transform, Quaternion value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = transform.localRotation;
        var end = relative ? start * value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToLocalRotation(transform);
    }

    public static MotionHandle DORotate(this Transform transform, Vector3 start, Vector3 end, float duration, Ease ease = Ease.OutQuad, bool relative = false, bool fast = true) {
        if (relative) {
            start += transform.eulerAngles;
            end += transform.eulerAngles;
        }
        if (fast) {
            end = new Vector3(
                start.x + Mathf.DeltaAngle(start.x, end.x),
                start.y + Mathf.DeltaAngle(start.y, end.y),
                start.z + Mathf.DeltaAngle(start.z, end.z)
            );
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToEulerAngles(transform);
    }

    public static MotionHandle DORotate(this Transform transform, Vector3 value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false, bool fast = true) {
        var start = transform.eulerAngles;
        var end = relative ? start + value : value;
        if (fast) {
            end = new Vector3(
                start.x + Mathf.DeltaAngle(start.x, end.x),
                start.y + Mathf.DeltaAngle(start.y, end.y),
                start.z + Mathf.DeltaAngle(start.z, end.z)
            );
        }
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToEulerAngles(transform);
    }

    public static MotionHandle DORotateX(this Transform transform, float start, float end, float duration, Ease ease = Ease.OutQuad, bool relative = false, bool fast = true) {
        if (relative) {
            start += transform.eulerAngles.x;
            end += transform.eulerAngles.x;
        }
        if (fast) {
            end = start + Mathf.DeltaAngle(start, end);
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToEulerAnglesX(transform);
    }

    public static MotionHandle DORotateX(this Transform transform, float value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false, bool fast = true) {
        var start = transform.eulerAngles.x;
        var end = relative ? start + value : value;
        if (fast) {
            end = start + Mathf.DeltaAngle(start, end);
        }
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToEulerAnglesX(transform);
    }

    public static MotionHandle DORotateY(this Transform transform, float start, float end, float duration, Ease ease = Ease.OutQuad, bool relative = false, bool fast = true) {
        if (relative) {
            start += transform.eulerAngles.y;
            end += transform.eulerAngles.y;
        }
        if (fast) {
            end = start + Mathf.DeltaAngle(start, end);
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToEulerAnglesY(transform);
    }

    public static MotionHandle DORotateY(this Transform transform, float value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false, bool fast = true) {
        var start = transform.eulerAngles.y;
        var end = relative ? start + value : value;
        if (fast) {
            end = start + Mathf.DeltaAngle(start, end);
        }
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToEulerAnglesY(transform);
    }

    public static MotionHandle DORotateZ(this Transform transform, float start, float end, float duration, Ease ease = Ease.OutQuad, bool relative = false, bool fast = true) {
        if (relative) {
            start += transform.eulerAngles.z;
            end += transform.eulerAngles.z;
        }
        if (fast) {
            end = start + Mathf.DeltaAngle(start, end);
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToEulerAnglesZ(transform);
    }

    public static MotionHandle DORotateZ(this Transform transform, float value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false, bool fast = true) {
        var start = transform.eulerAngles.z;
        var end = relative ? start + value : value;
        if (fast) {
            end = start + Mathf.DeltaAngle(start, end);
        }
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToEulerAnglesZ(transform);
    }

    public static MotionHandle DOLocalRotate(this Transform transform, Vector3 start, Vector3 end, float duration, Ease ease = Ease.OutQuad, bool relative = false, bool fast = true) {
        if (relative) {
            start += transform.localEulerAngles;
            end += transform.localEulerAngles;
        }
        if (fast) {
            end = new Vector3(
                start.x + Mathf.DeltaAngle(start.x, end.x),
                start.y + Mathf.DeltaAngle(start.y, end.y),
                start.z + Mathf.DeltaAngle(start.z, end.z)
            );
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToLocalEulerAngles(transform);
    }

    public static MotionHandle DOLocalRotate(this Transform transform, Vector3 value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false, bool fast = true) {
        var start = transform.localEulerAngles;
        var end = relative ? start + value : value;
        if (fast) {
            end = new Vector3(
                start.x + Mathf.DeltaAngle(start.x, end.x),
                start.y + Mathf.DeltaAngle(start.y, end.y),
                start.z + Mathf.DeltaAngle(start.z, end.z)
            );
        }
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToLocalEulerAngles(transform);
    }

    public static MotionHandle DOLocalRotateX(this Transform transform, float start, float end, float duration, Ease ease = Ease.OutQuad, bool relative = false, bool fast = true) {
        if (relative) {
            start += transform.localEulerAngles.x;
            end += transform.localEulerAngles.x;
        }
        if (fast) {
            end = start + Mathf.DeltaAngle(start, end);
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToLocalEulerAnglesX(transform);
    }

    public static MotionHandle DOLocalRotateX(this Transform transform, float value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false, bool fast = true) {
        var start = transform.localEulerAngles.x;
        var end = relative ? start + value : value;
        if (fast) {
            end = start + Mathf.DeltaAngle(start, end);
        }
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToLocalEulerAnglesX(transform);
    }

    public static MotionHandle DOLocalRotateY(this Transform transform, float start, float end, float duration, Ease ease = Ease.OutQuad, bool relative = false, bool fast = true) {
        if (relative) {
            start += transform.localEulerAngles.y;
            end += transform.localEulerAngles.y;
        }
        if (fast) {
            end = start + Mathf.DeltaAngle(start, end);
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToLocalEulerAnglesY(transform);
    }

    public static MotionHandle DOLocalRotateY(this Transform transform, float value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false, bool fast = true) {
        var start = transform.localEulerAngles.y;
        var end = relative ? start + value : value;
        if (fast) {
            end = start + Mathf.DeltaAngle(start, end);
        }
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToLocalEulerAnglesY(transform);
    }

    public static MotionHandle DOLocalRotateZ(this Transform transform, float start, float end, float duration, Ease ease = Ease.OutQuad, bool relative = false, bool fast = true) {
        if (relative) {
            start += transform.localEulerAngles.z;
            end += transform.localEulerAngles.z;
        }
        if (fast) {
            end = start + Mathf.DeltaAngle(start, end);
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToLocalEulerAnglesZ(transform);
    }

    public static MotionHandle DOLocalRotateZ(this Transform transform, float value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false, bool fast = true) {
        var start = transform.localEulerAngles.z;
        var end = relative ? start + value : value;
        if (fast) {
            end = start + Mathf.DeltaAngle(start, end);
        }
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToLocalEulerAnglesZ(transform);
    }

    public static MotionHandle DOScale(this Transform transform, Vector3 start, Vector3 end, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            start += transform.localScale;
            end += transform.localScale;
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToLocalScale(transform);
    }

    public static MotionHandle DOScale(this Transform transform, Vector3 value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = transform.localScale;
        var end = relative ? start + value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToLocalScale(transform);
    }

    public static MotionHandle DOScale(this Transform transform, float start, float end, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            start += transform.localScale.x;
            end += transform.localScale.x;
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToLocalScaleXYZ(transform);
    }

    public static MotionHandle DOScale(this Transform transform, float value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = transform.localScale.x;
        var end = relative ? start + value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToLocalScaleXYZ(transform);
    }

    public static MotionHandle DOScaleX(this Transform transform, float start, float end, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            start += transform.localScale.x;
            end += transform.localScale.x;
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToLocalScaleX(transform);
    }

    public static MotionHandle DOScaleX(this Transform transform, float value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = transform.localScale.x;
        var end = relative ? start + value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToLocalScaleX(transform);
    }

    public static MotionHandle DOScaleY(this Transform transform, float start, float end, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            start += transform.localScale.y;
            end += transform.localScale.y;
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToLocalScaleY(transform);
    }

    public static MotionHandle DOScaleY(this Transform transform, float value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = transform.localScale.y;
        var end = relative ? start + value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToLocalScaleY(transform);
    }

    public static MotionHandle DOScaleZ(this Transform transform, float start, float end, float duration, Ease ease = Ease.OutQuad, bool relative = false) {
        if (relative) {
            start += transform.localScale.z;
            end += transform.localScale.z;
        }
        return LMotion.Create(start, end, duration).WithEase(ease).WithCancelOnError().BindToLocalScaleZ(transform);
    }

    public static MotionHandle DOScaleZ(this Transform transform, float value, float duration, Ease ease = Ease.OutQuad, bool from = false, bool relative = false) {
        var start = transform.localScale.z;
        var end = relative ? start + value : value;
        var lm = from ? LMotion.Create(end, start, duration) : LMotion.Create(start, end, duration);
        return lm.WithEase(ease).WithCancelOnError().BindToLocalScaleZ(transform);
    }
    #endregion
}
}
