import { ActionIcon, Card, Group, Input, SimpleGrid, Stack, Text } from '@mantine/core'
import { useClipboard } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiDeleteOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC } from 'react'
import { useTranslation } from 'react-i18next'
import { useDisplayInputStyles } from '@Utils/ThemeOverride'
import { Attachment, FlagInfoModel } from '@Api'

interface FlagCardProps {
  flag: FlagInfoModel
  onDelete: () => void
  unifiedAttachment?: Attachment | null
}

const FlagCard: FC<FlagCardProps> = ({ flag, onDelete, unifiedAttachment }) => {
  const clipboard = useClipboard()
  const attachment = unifiedAttachment ?? flag.attachment
  const shortURL = attachment?.url?.split('/').slice(-2)[0].slice(0, 8)
  const { classes } = useDisplayInputStyles({ fw: 'bold', ff: 'monospace' })
  const { t } = useTranslation()

  return (
    <Card p="sm">
      <Group wrap="nowrap" justify="space-between" gap={3}>
        <Stack align="flex-start" gap={0} w="100%">
          <Input
            variant="unstyled"
            value={flag.flag}
            w="100%"
            size="md"
            readOnly
            onClick={() => {
              clipboard.copy(flag.flag)
              showNotification({
                message: t('admin.notification.games.challenges.flag.copied'),
                color: 'teal',
                icon: <Icon path={mdiCheck} size={1} />,
              })
            }}
            classNames={classes}
            styles={{ input: { cursor: 'pointer' } }}
          />
          <Text c="dimmed" size="sm" ff="monospace">
            {attachment?.type} {shortURL}
          </Text>
        </Stack>
        <ActionIcon onClick={onDelete} color="red">
          <Icon path={mdiDeleteOutline} size={1} />
        </ActionIcon>
      </Group>
    </Card>
  )
}

interface FladEditPanelProps {
  flags?: FlagInfoModel[]
  onDelete: (flag: FlagInfoModel) => void
  unifiedAttachment?: Attachment | null
}

const FladEditPanel: FC<FladEditPanelProps> = ({ flags, onDelete, unifiedAttachment }) => {
  return (
    <Stack>
      <SimpleGrid spacing="sm" cols={{ base: 2, w18: 3, w24: 4, w30: 5, w36: 6, w42: 7, w48: 8 }}>
        {flags &&
          flags.map((flag, i) => (
            <FlagCard
              key={i}
              flag={flag}
              onDelete={() => onDelete(flag)}
              unifiedAttachment={unifiedAttachment}
            />
          ))}
      </SimpleGrid>
    </Stack>
  )
}

export default FladEditPanel
