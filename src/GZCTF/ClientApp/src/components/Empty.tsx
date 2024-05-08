import { MantineSize, Stack, Text } from '@mantine/core'
import { mdiInbox } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

interface EmptyProps {
  bordered?: boolean
  description?: ReactNode
  fontSize?: string | MantineSize | undefined
  mdiPath?: string
  iconSize?: number
}

const Empty: FC<EmptyProps> = (props) => {
  const { t } = useTranslation()

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
      <Text c="dimmed" size={props.fontSize}>
        {props.description ?? t('common.content.no_data')}
      </Text>
    </Stack>
  )
}

export default Empty
