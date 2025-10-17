import { GamePermission } from '@Api'

export interface PermissionDefinition {
  value: GamePermission
  challengeScoped: boolean
  i18nKey: string
  color: string
}

export const PERMISSION_DEFINITIONS: PermissionDefinition[] = [
  {
    value: GamePermission.JoinGame,
    challengeScoped: false,
    i18nKey: 'join_game',
    color: 'grape',
  },
  {
    value: GamePermission.RankOverall,
    challengeScoped: false,
    i18nKey: 'rank_overall',
    color: 'violet',
  },
  {
    value: GamePermission.ViewChallenge,
    challengeScoped: true,
    i18nKey: 'view_challenge',
    color: 'yellow',
  },
  {
    value: GamePermission.SubmitFlags,
    challengeScoped: true,
    i18nKey: 'submit_flags',
    color: 'green',
  },
  {
    value: GamePermission.GetScore,
    challengeScoped: true,
    i18nKey: 'get_score',
    color: 'blue',
  },
  {
    value: GamePermission.GetBlood,
    challengeScoped: true,
    i18nKey: 'get_blood',
    color: 'red',
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
