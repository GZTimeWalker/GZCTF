import { showNotification } from '@mantine/notifications'
import { showErrorMsg } from '@Utils/Shared'
import { resolveSkillTreeErrorMessageKey } from '@Utils/skillTreeAdmin'

export const showSkillTreeError = (error: unknown, t: (key: string) => string) => {
  const key = resolveSkillTreeErrorMessageKey(error)
  if (key) {
    showNotification({
      color: 'red',
      title: t('skillTrees:errors.generic'),
      message: t(key),
    })
    return
  }

  showErrorMsg(error, t)
}
