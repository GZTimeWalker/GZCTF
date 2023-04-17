import { showNotification } from '@mantine/notifications'
import { mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'

export const showErrorNotification = (err: any) => {
  if (err?.response?.status === 429) {
    showNotification({
      color: 'red',
      message: '操作过于频繁，请稍后再试',
      icon: <Icon path={mdiClose} size={1} />,
      withCloseButton: false,
    })
    return
  }

  console.warn(err)
  showNotification({
    color: 'red',
    title: '遇到了问题',
    message: `${err?.response?.data?.title ?? err?.title ?? err ?? '未知错误'}`,
    icon: <Icon path={mdiClose} size={1} />,
    withCloseButton: false,
  })
}
