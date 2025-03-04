using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public struct UniTaskTokenID : IComparable, IComparable<UniTaskTokenID>, IEquatable<UniTaskTokenID>
{
    public Guid ID;

    public bool IsEmpty => ID == Guid.Empty;
    public void CheckValue()
    {
        if (IsEmpty)
            ID = Guid.NewGuid();
    }
    public int CompareTo(object value)
    {
        if (value == null)
            return 1;
        if (!(value is UniTaskTokenID guid))
            return 0;
        return ID.CompareTo(guid.ID);
    }

    public int CompareTo(UniTaskTokenID other)
    {
        return ID.CompareTo(other.ID);
    }

    public bool Equals(UniTaskTokenID other)
    {
        return ID.Equals(other.ID);
    }
}

// CancellationToken을 중앙에서 관리하기 위해 제작
// 각 class 에서 class 형태의  CancellationToken을 들고 있는 형태 대신에 struct 방식의 UniTaskTokenID를 들고 있어서 작동하게 변경
public static partial class UniTaskManager
{
    private static Dictionary<UniTaskTokenID, CancellationTokenSource> m_dicCancelToken = new Dictionary<UniTaskTokenID, CancellationTokenSource>();
    
    private static UniTaskTokenID _CreateTokenID()
    {
        return new UniTaskTokenID() { ID = Guid.NewGuid() };
    }
    public static CancellationTokenSource CreateCancelToken(out UniTaskTokenID tokenID)
    {
        tokenID = _CreateTokenID();
        if (m_dicCancelToken.TryGetValue(tokenID, out var token))
        {   //혹시 Guid가 중복되어 생성되었을 경우 예외처리
            token.Cancel();
            token.Dispose();
            token = new CancellationTokenSource();
            return token;
        }

        token = new CancellationTokenSource();
        m_dicCancelToken[tokenID] = token;
        return token;
    }
    //캐싱된 ID 값이 있을 경우 해당 ID 값을 활용
    public static CancellationTokenSource CreateCancelToken(UniTaskTokenID tokenID)
    {
        if (tokenID.IsEmpty)
            return null;
        
        if (m_dicCancelToken.TryGetValue(tokenID, out var token))
        {   //캐싱된 Guid 이므로 같이 사용하는 Token으로 인지
            return token;
        }

        token = new CancellationTokenSource();
        m_dicCancelToken[tokenID] = token;
        return token;
    }

    public static void CancelToken(UniTaskTokenID tokenID)
    {
        if (tokenID.IsEmpty)
            return;
        
        if (m_dicCancelToken.TryGetValue(tokenID, out var token) == false)
        {   //토큰이 존재하지 않는 경우 예외처리
            return;
        }
        
        token.Cancel();
        token.Dispose();
        m_dicCancelToken.Remove(tokenID);
    }

    public static void DisposeToken(UniTaskTokenID tokenID)
    {
        if (tokenID.IsEmpty)
            return;
        
        if (m_dicCancelToken.TryGetValue(tokenID, out var token) == false)
        {   //토큰이 존재하지 않는 경우 예외처리
            return;
        }
        
        token.Dispose();
        m_dicCancelToken.Remove(tokenID);
    }
}
