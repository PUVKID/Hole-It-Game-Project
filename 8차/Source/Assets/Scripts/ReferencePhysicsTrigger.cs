using UnityEngine;

/// <summary>원본처럼 실제 물리 접촉 이벤트로 활성화·재질·수집을 처리합니다.</summary>
public sealed class ReferencePhysicsTrigger : MonoBehaviour {
    public enum TriggerKind { Activate, Smooth, Collect }
    public ReferenceGame Owner;
    public TriggerKind Kind;
    void OnTriggerEnter(Collider other){if(Owner!=null)Owner.HandleReferenceTrigger(other,Kind,true);}
    void OnTriggerExit(Collider other){if(Owner!=null)Owner.HandleReferenceTrigger(other,Kind,false);}
}
