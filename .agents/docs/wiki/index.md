# ExplosiveFactory 시스템 위키 (Wiki Index)

> 세션 간 지식을 축적하여 코드를 일일이 분석하지 않고도 전체 시스템 아키텍처와 흐름을 즉시 파악할 수 있도록 돕는 위키 인덱스입니다.
> **작업 착수 전 관련 시스템 문서를 필독합니다.**

---

## 📚 위키 카테고리 목차

| 카테고리 | 문서 링크 | 다루는 주요 시스템 및 내용 |
|---|---|---|
| **Item System** | [wiki/item.md](item.md) | 아이템 생명주기(`Grounded` / `Held` / `Thrown`), `ItemHolder` 슬롯 전환, `ItemSo` 데이터 파이프라인, `ItemVendingMachine` 자판기 상호작용 |
| **Player System** | [wiki/player.md](player.md) | 1인칭 FPS 물리 이동(`PlayerMove`), 마우스 회전/시선 동기화(`PlayerRotate`), `InteractiveRaycast` 인터랙션, 손/몸체 애니메이션 |
| **Network & Steam** | [wiki/network.md](network.md) | Steam 로비 매칭, `CustomNetworkManager` 프리팹 자동 등록, 씬 전환 및 동적 오브젝트 스폰 흐름, `GamePlayer` 상태 동기화 |

---

## 🛠️ 빠른 참조 링크
- [폴더 및 스크립트 색인 (layout.md)](../layout.md)
- [기술 스택 및 API 컨벤션 (stack.md)](../stack.md)
- [신규 아이템 생성 스킬](../../skills/create-item/SKILL.md)
