import { showNotification } from '@mantine/notifications'
import { mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'

function showErrorNotification(err: any) {
  showNotification({
    color: 'red',
    title: '遇到了问题',
    message: `${err.response.data.title}`,
    icon: <Icon path={mdiClose} size={1} />,
    disallowClose: true,
  })
}

export { showErrorNotification }
