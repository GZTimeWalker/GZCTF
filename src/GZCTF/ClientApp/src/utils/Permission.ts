import { MantineColor } from '@mantine/core'
import { GamePermission } from '@Api'

export interface PermissionDefinition {
  value: GamePermission
  challengeScoped: boolean
  i18nKey: string
  color: MantineColor
}

export const PERMISSION_DEFINITIONS: PermissionDefinition[] = [
  {
    value: GamePermission.JoinGame,
    challengeScoped: false,
    i18nKey: 'join_game',
    color: 'red',
  },
  {
    value: GamePermission.RankOverall,
    challengeScoped: false,
    i18nKey: 'rank_overall',
    color: 'pink',
  },
  {
    value: GamePermission.RequireReview,
    challengeScoped: false,
    i18nKey: 'require_review',
    color: 'grape',
  },
  {
    value: GamePermission.ViewChallenge,
    challengeScoped: true,
    i18nKey: 'view_challenge',
    color: 'indigo',
  },
  {
    value: GamePermission.SubmitFlags,
    challengeScoped: true,
    i18nKey: 'submit_flags',
    color: 'blue',
  },
  {
    value: GamePermission.GetScore,
    challengeScoped: true,
    i18nKey: 'get_score',
    color: 'green',
  },
  {
    value: GamePermission.GetBlood,
    challengeScoped: true,
    i18nKey: 'get_blood',
    color: 'yellow',
  },
  {
    value: GamePermission.AffectDynamicScore,
    challengeScoped: true,
    i18nKey: 'affect_dynamic_score',
    color: 'orange',
  },
]

export const CHALLENGE_SCOPED_PERMISSIONS = PERMISSION_DEFINITIONS.filter((definition) => definition.challengeScoped)

export const getPermissionI18nKey = (key: string, type: 'label' | 'description') =>
  `admin.content.games.divisions.permissions.${key}.${type}`

export const ALL_PERMISSIONS_MASK = GamePermission.All

export const permissionMaskToArray = (mask?: number | null): GamePermission[] => {
  const effectiveMask = mask ?? ALL_PERMISSIONS_MASK
  if (effectiveMask === GamePermission.All) {
    return PERMISSION_DEFINITIONS.map((item) => item.value)
  }

  return PERMISSION_DEFINITIONS.filter((item) => (effectiveMask & item.value) === item.value).map((item) => item.value)
}

export const permissionsToMask = (permissions: Iterable<GamePermission>): number => {
  const values = Array.from(permissions)
  if (values.length === PERMISSION_DEFINITIONS.length) {
    return GamePermission.All
  }

  return values.reduce((mask, value) => mask | value, 0)
}

export const hasPermission = (mask: number | undefined | null, permission: GamePermission): boolean => {
  if (mask === undefined || mask === null) {
    return true
  }

  if (mask === GamePermission.All) {
    return true
  }

  return (mask & permission) === permission
}
