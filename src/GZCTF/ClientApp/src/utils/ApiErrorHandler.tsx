import { showNotification } from '@mantine/notifications'
import { mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import { I18nKey } from './I18n'

export const tryGetErrorMsg = (err: any, t: (key: I18nKey) => string) => {
  return err?.response?.data?.title ?? err?.title ?? err ?? t('Error_Unknown')
}

export const showErrorNotification = (err: any, t: (key: I18nKey) => string) => {
  if (err?.response?.status === 429) {
    showNotification({
      color: 'red',
      message: t('Request_TooFrequent'),
      icon: <Icon path={mdiClose} size={1} />,
    })
    return
  }

  console.warn(err)
  showNotification({
    color: 'red',
    title: t('Error_Encountered'),
    message: tryGetErrorMsg(err, t),
    icon: <Icon path={mdiClose} size={1} />,
  })
}
