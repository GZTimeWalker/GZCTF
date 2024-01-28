import { showNotification } from '@mantine/notifications'
import { mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'

export const tryGetErrorMsg = (err: any, t: (key: string) => string) => {
  return err?.response?.data?.title ?? err?.title ?? err ?? t('common.error.unknown')
}

export const showErrorNotification = (err: any, t: (key: string) => string) => {
  if (err?.response?.status === 429) {
    showNotification({
      color: 'red',
      message: t('common.error.request_too_frequent'),
      icon: <Icon path={mdiClose} size={1} />,
    })
    return
  }

  console.warn(err)
  showNotification({
    color: 'red',
    title: t('common.error.encountered'),
    message: tryGetErrorMsg(err, t),
    icon: <Icon path={mdiClose} size={1} />,
  })
}
