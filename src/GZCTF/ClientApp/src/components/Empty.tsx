import { FC, ReactNode } from 'react'
import { MantineNumberSize, Stack, Text } from '@mantine/core'
import { mdiInbox } from '@mdi/js'
import { Icon } from '@mdi/react'

interface EmptyProps {
  bordered?: boolean
  description?: ReactNode
  fontSize?: MantineNumberSize
  mdiPath?: string
  iconSize?: number
}

const Empty: FC<EmptyProps> = (props) => {
  return (
    <Stack
      align="center"
      sx={
        props.bordered
          ? {
              borderStyle: 'dashed',
              borderColor: 'gray',
              borderWidth: 'thick',
              borderRadius: '1rem',
              padding: '1rem',
            }
          : undefined
      }
    >
      <Icon path={props.mdiPath ?? mdiInbox} size={props.iconSize ?? 4} color="gray" />
      <Text size={props.fontSize}>{props.description ?? '没有数据'}</Text>
    </Stack>
  )
}

export default Empty
