import { FC } from 'react'
import {
  ActionIcon,
  Group,
  Stack,
  Text,
  Card,
  useMantineTheme,
  SimpleGrid,
  Input,
} from '@mantine/core'
import { useClipboard } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiDeleteOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { Attachment, FlagInfoModel } from '@Api'

interface FlagCardProps {
  flag: FlagInfoModel
  onDelete: () => void
  unifiedAttachment?: Attachment | null
}

const FlagCard: FC<FlagCardProps> = ({ flag, onDelete, unifiedAttachment }) => {
  const theme = useMantineTheme()
  const clipboard = useClipboard()
  const attachment = unifiedAttachment ?? flag.attachment
  const shortURL = attachment?.url?.split('/').slice(-2)[0].slice(0, 8)

  return (
    <Card>
      <Group noWrap position="apart" spacing={3}>
        <Stack align="flex-start" spacing={0} style={{ width: '100%' }}>
          <Input
            variant="unstyled"
            value={flag.flag}
            size="md"
            readOnly
            onClick={() => {
              clipboard.copy(flag.flag)
              showNotification({
                message: 'flag 已复制到剪贴板',
                color: 'teal',
                icon: <Icon path={mdiCheck} size={1} />,
                disallowClose: true,
              })
            }}
            styles={{
              input: {
                fontFamily: theme.fontFamilyMonospace,
              },
              wrapper: {
                width: '100%',
              },
            }}
          />
          <Text color="dimmed" size="sm" style={{ fontFamily: theme.fontFamilyMonospace }}>
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
      <SimpleGrid
        cols={2}
        breakpoints={[
          { maxWidth: 3600, cols: 4, spacing: 'sm' },
          { maxWidth: 2700, cols: 3, spacing: 'sm' },
          { maxWidth: 1800, cols: 2, spacing: 'sm' },
        ]}
      >
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
