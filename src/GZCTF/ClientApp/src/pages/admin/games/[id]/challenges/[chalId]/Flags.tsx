import {
  Button,
  Center,
  Chip,
  Code,
  Divider,
  FileButton,
  Group,
  Input,
  List,
  Overlay,
  Progress,
  ScrollArea,
  Stack,
  Text,
  TextInput,
  Title,
  alpha,
  useMantineColorScheme,
  useMantineTheme,
} from '@mantine/core'
import { useModals } from '@mantine/modals'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiPuzzleEditOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useEffect, useState } from 'react'
import { Trans, useTranslation } from 'react-i18next'
import { useNavigate, useParams } from 'react-router-dom'
import AttachmentRemoteEditModal from '@Components/admin/AttachmentRemoteEditModal'
import AttachmentUploadModal from '@Components/admin/AttachmentUploadModal'
import FlagCreateModal from '@Components/admin/FlagCreateModal'
import FlagEditPanel from '@Components/admin/FlagEditPanel'
import WithChallengeEdit from '@Components/admin/WithChallengeEdit'
import { showErrorNotification } from '@Utils/ApiHelper'
import { useDisplayInputStyles } from '@Utils/ThemeOverride'
import { useEditChallenge } from '@Utils/useEdit'
import api, { ChallengeType, FileType, FlagInfoModel } from '@Api'
import uploadClasses from '@Styles/Upload.module.css'

interface FlagEditProps {
  onDelete: (flag: FlagInfoModel) => void
}

// with only one attachment
const OneAttachmentWithFlags: FC<FlagEditProps> = ({ onDelete }) => {
  const { id, chalId } = useParams()
  const [numId, numCId] = [parseInt(id ?? '-1'), parseInt(chalId ?? '-1')]

  const { challenge, mutate } = useEditChallenge(numId, numCId)

  const [disabled, setDisabled] = useState(false)
  const [type, setType] = useState<FileType>(challenge?.attachment?.type ?? FileType.None)
  const [remoteUrl, setRemoteUrl] = useState(challenge?.attachment?.url ?? '')
  const [flagTemplate, setFlagTemplate] = useState(challenge?.flagTemplate ?? '')

  const modals = useModals()
  const { colorScheme } = useMantineColorScheme()
  const { t } = useTranslation()

  useEffect(() => {
    if (challenge) {
      setType(challenge.attachment?.type ?? FileType.None)
      setRemoteUrl(challenge.attachment?.url ?? '')
      setFlagTemplate(challenge.flagTemplate ?? '')
    }
  }, [challenge])

  const onConfirmClear = () => {
    setDisabled(true)
    api.edit
      .editUpdateAttachment(numId, numCId, { attachmentType: FileType.None })
      .then(() => {
        showNotification({
          color: 'teal',
          message: t('admin.notification.games.challenges.attachment.updated'),
          icon: <Icon path={mdiCheck} size={1} />,
        })
        setType(FileType.None)
        challenge &&
          mutate({
            ...challenge,
            attachment: null,
          })
      })
      .catch((err) => showErrorNotification(err, t))
      .finally(() => {
        setDisabled(false)
      })
  }

  const theme = useMantineTheme()
  const [progress, setProgress] = useState(0)
  const [flagCreateModalOpen, setFlagCreateModalOpen] = useState(false)
  const FileTypeDesrcMap = new Map<FileType, string>([
    [FileType.None, t('challenge.file_type.none')],
    [FileType.Remote, t('challenge.file_type.remote')],
    [FileType.Local, t('challenge.file_type.local')],
  ])

  const onUpload = (file: File | null) => {
    if (!file) return

    setProgress(0)
    setDisabled(true)

    api.assets
      .assetsUpload(
        {
          files: [file],
        },
        undefined,
        {
          onUploadProgress: (e) => {
            setProgress((e.loaded / (e.total ?? 1)) * 90)
          },
        }
      )
      .then((data) => {
        const file = data.data[0]
        setProgress(95)
        if (file) {
          api.edit
            .editUpdateAttachment(numId, numCId, {
              attachmentType: FileType.Local,
              fileHash: file.hash,
            })
            .then(() => {
              setProgress(0)
              setDisabled(false)
              mutate()
              showNotification({
                color: 'teal',
                message: t('admin.notification.games.challenges.attachment.updated'),
                icon: <Icon path={mdiCheck} size={1} />,
              })
            })
            .catch((err) => showErrorNotification(err, t))
            .finally(() => {
              setDisabled(false)
            })
        }
      })
      .catch((err) => showErrorNotification(err, t))
      .finally(() => {
        setDisabled(false)
      })
  }

  const onRemote = () => {
    if (!remoteUrl.startsWith('http')) return
    setDisabled(true)
    api.edit
      .editUpdateAttachment(numId, numCId, {
        attachmentType: FileType.Remote,
        remoteUrl: remoteUrl,
      })
      .then(() => {
        showNotification({
          color: 'teal',
          message: t('admin.notification.games.challenges.attachment.updated'),
          icon: <Icon path={mdiCheck} size={1} />,
        })
      })
      .catch((err) => showErrorNotification(err, t))
      .finally(() => {
        setDisabled(false)
      })
  }

  const onChangeFlagTemplate = () => {
    if (flagTemplate === challenge?.flagTemplate) return

    setDisabled(true)
    api.edit
      // allow empty flag template to be set (but not null or undefined)
      .editUpdateGameChallenge(numId, numCId, { flagTemplate })
      .then(() => {
        showNotification({
          color: 'teal',
          message: t('admin.notification.games.challenges.flag_template.updated'),
          icon: <Icon path={mdiCheck} size={1} />,
        })
        challenge && mutate({ ...challenge, flagTemplate: flagTemplate })
      })
      .catch((e) => showErrorNotification(e, t))
      .finally(() => {
        setDisabled(false)
      })
  }

  const will_generate =
    ' ' + t('admin.content.games.challenges.flag.instructions.will_generate') + ' '

  return (
    <Stack>
      <Group justify="space-between">
        <Title order={2}>{t('admin.content.games.challenges.attachment.title')}</Title>
        {type !== FileType.Remote ? (
          <FileButton onChange={onUpload}>
            {(props) => (
              <Button
                {...props}
                fullWidth
                className={uploadClasses.button}
                disabled={type !== FileType.Local}
                w="122px"
                mt="24px"
                color={progress !== 0 ? 'cyan' : theme.primaryColor}
              >
                <div className={uploadClasses.label}>
                  {progress !== 0
                    ? t('admin.button.challenges.attachment.uploading')
                    : t('admin.button.challenges.attachment.upload')}
                </div>
                {progress !== 0 && (
                  <Progress
                    value={progress}
                    className={uploadClasses.progress}
                    color={alpha(theme.colors[theme.primaryColor][2], 0.35)}
                    radius="sm"
                  />
                )}
              </Button>
            )}
          </FileButton>
        ) : (
          <Button disabled={disabled} w="122px" mt="24px" onClick={onRemote}>
            {t('admin.button.challenges.attachment.save_url')}
          </Button>
        )}
      </Group>
      <Divider />
      <Group justify="space-between" wrap="nowrap">
        <Input.Wrapper label={t('admin.content.games.challenges.attachment.type')} required>
          <Chip.Group
            value={type}
            onChange={(e) => {
              if (e === FileType.None) {
                modals.openConfirmModal({
                  title: t('admin.content.games.challenges.attachment.clear.title'),
                  children: (
                    <Text size="sm">
                      {t('admin.content.games.challenges.attachment.clear.description')}
                    </Text>
                  ),
                  onConfirm: onConfirmClear,
                  confirmProps: { color: 'orange' },
                })
              } else {
                setType(e as FileType)
              }
            }}
          >
            <Group justify="left" gap="sm" h="2.25rem" wrap="nowrap">
              {Object.entries(FileType).map((type) => (
                <Chip key={type[0]} value={type[1]} size="sm">
                  {FileTypeDesrcMap.get(type[1])}
                </Chip>
              ))}
            </Group>
          </Chip.Group>
        </Input.Wrapper>
        {type !== FileType.Remote ? (
          <TextInput
            label={t('admin.content.games.challenges.attachment.link')}
            readOnly
            disabled={disabled || type === FileType.None}
            value={challenge?.attachment?.url ?? ''}
            w="calc(100% - 400px)"
            classNames={{ input: uploadClasses.hover }}
            onClick={() =>
              challenge?.attachment?.url && window.open(challenge?.attachment?.url, '_blank')
            }
          />
        ) : (
          <TextInput
            label={t('admin.content.games.challenges.attachment.link')}
            disabled={disabled}
            value={remoteUrl}
            w="calc(100% - 400px)"
            classNames={{ input: uploadClasses.hover }}
            onChange={(e) => setRemoteUrl(e.target.value)}
          />
        )}
      </Group>
      <Group justify="space-between" mt={20}>
        <Title order={2}>{t('admin.content.games.challenges.flag.title')}</Title>
        {challenge?.type === ChallengeType.DynamicContainer ? (
          <Button disabled={disabled} onClick={onChangeFlagTemplate}>
            {t('admin.button.challenges.flag.save')}
          </Button>
        ) : (
          <Button disabled={disabled} w="122px" onClick={() => setFlagCreateModalOpen(true)}>
            {t('admin.button.challenges.flag.add.normal')}
          </Button>
        )}
      </Group>
      <Divider />
      {challenge?.type === ChallengeType.DynamicContainer ? (
        <Stack>
          <TextInput
            label={t('admin.content.games.challenges.flag.template')}
            size="sm"
            value={flagTemplate}
            placeholder="flag{[GUID]}"
            onChange={(e) => setFlagTemplate(e.target.value)}
            styles={{
              input: {
                fontFamily: theme.fontFamilyMonospace,
              },
            }}
          />
          <Stack gap={6} pb={8}>
            <Text size="sm">
              {t('admin.content.games.challenges.flag.instructions.description')}
            </Text>
            <Text size="sm">
              <Trans i18nKey="admin.content.games.challenges.flag.instructions.guid">
                _<Code>_</Code>_
              </Trans>
            </Text>
            <Text size="sm">
              <Trans i18nKey="admin.content.games.challenges.flag.instructions.team_hash">
                _<Code>_</Code>_
              </Trans>
            </Text>
            <Text size="sm">
              <Trans i18nKey="admin.content.games.challenges.flag.instructions.leet">
                _<Code>_</Code>_
              </Trans>
            </Text>
            <Text size="sm">
              <Trans i18nKey="admin.content.games.challenges.flag.instructions.both">
                _<Code>_</Code>
                <Code>_</Code>_
              </Trans>
            </Text>
            <Text size="sm">
              <Trans i18nKey="admin.content.games.challenges.flag.instructions.complex">
                _<Code>_</Code>
                <Code>_</Code>_
              </Trans>
            </Text>
            <Text size="sm" fw="bold">
              {t('admin.content.games.challenges.flag.instructions.example')}
            </Text>
            <List size="sm" spacing={6}>
              <List.Item>
                {t('admin.content.games.challenges.flag.instructions.leave_empty')}
                {will_generate}
                <Code>{`flag{1bab71b8-117f-4dea-a047-340b72101d7b}`}</Code>
              </List.Item>
              <List.Item>
                <Code>{`flag{hello world}`}</Code>
                {will_generate}
                <Code>{`flag{He1lo_w0r1d}`}</Code>
              </List.Item>
              <List.Item>
                <Code>{`[CLEET]flag{hello sara}`}</Code>
                {will_generate}
                <Code>{`flag{He1!o_$@rA}`}</Code>
              </List.Item>
              <List.Item>
                <Code>{`flag{hello_world_[TEAM_HASH]}`}</Code>
                {will_generate}
                <Code>{`flag{hello_world_5418ce4d815c}`}</Code>
              </List.Item>
              <List.Item>
                <Code>{`[LEET]flag{hello world [TEAM_HASH]}`}</Code>
                {will_generate}
                <Code>{`flag{He1lo_w0r1d_5418ce4d815c}`}</Code>
              </List.Item>
            </List>
          </Stack>
        </Stack>
      ) : (
        <ScrollArea h="calc(100vh - 32rem)" pos="relative">
          {!challenge?.flags.length && (
            <>
              <Overlay opacity={0.3} color={colorScheme === 'dark' ? 'black' : 'white'} />
              <Center h="calc(100vh - 32rem)">
                <Stack gap={0}>
                  <Title order={2}>{t('admin.content.games.challenges.flag.empty.title')}</Title>
                  <Text>{t('admin.content.games.challenges.flag.empty.description')}</Text>
                </Stack>
              </Center>
            </>
          )}
          <FlagEditPanel
            flags={challenge?.flags}
            onDelete={onDelete}
            unifiedAttachment={challenge?.attachment}
          />
        </ScrollArea>
      )}
      <FlagCreateModal
        title={t('admin.button.challenges.flag.add.normal')}
        opened={flagCreateModalOpen}
        onClose={() => setFlagCreateModalOpen(false)}
      />
    </Stack>
  )
}

const FlagsWithAttachments: FC<FlagEditProps> = ({ onDelete }) => {
  const { id, chalId } = useParams()
  const [numId, numCId] = [parseInt(id ?? '-1'), parseInt(chalId ?? '-1')]
  const { challenge } = useEditChallenge(numId, numCId)

  const [attachmentUploadModalOpened, setAttachmentUploadModalOpened] = useState(false)
  const [remoteAttachmentModalOpened, setRemoteAttachmentModalOpened] = useState(false)

  const { colorScheme } = useMantineColorScheme()
  const { t } = useTranslation()

  return (
    <Stack>
      <Group justify="space-between" mt={20}>
        <Title order={2}>{t('admin.content.games.challenges.flag.title')}</Title>
        <Group justify="right">
          <Button onClick={() => setRemoteAttachmentModalOpened(true)}>
            {t('admin.button.challenges.flag.add.remote')}
          </Button>
          <Button onClick={() => setAttachmentUploadModalOpened(true)}>
            {t('admin.button.challenges.flag.add.dynamic')}
          </Button>
        </Group>
      </Group>
      <Divider />
      <ScrollArea h="calc(100vh - 25rem)" pos="relative">
        {!challenge?.flags.length && (
          <>
            <Overlay opacity={0.3} color={colorScheme === 'dark' ? 'black' : 'white'} />
            <Center h="calc(100vh - 25rem)">
              <Stack gap={0}>
                <Title order={2}>{t('admin.content.games.challenges.flag.empty.title')}</Title>
                <Text>{t('admin.content.games.challenges.flag.empty.description')}</Text>
              </Stack>
            </Center>
          </>
        )}
        <FlagEditPanel flags={challenge?.flags} onDelete={onDelete} />
      </ScrollArea>
      <AttachmentUploadModal
        title={t('admin.button.challenges.flag.add.dynamic')}
        size="40%"
        opened={attachmentUploadModalOpened}
        onClose={() => setAttachmentUploadModalOpened(false)}
      />
      <AttachmentRemoteEditModal
        title={t('admin.button.challenges.flag.add.remote')}
        size="40%"
        opened={remoteAttachmentModalOpened}
        onClose={() => setRemoteAttachmentModalOpened(false)}
      />
    </Stack>
  )
}

const GameChallengeEdit: FC = () => {
  const navigate = useNavigate()
  const { id, chalId } = useParams()
  const [numId, numCId] = [parseInt(id ?? '-1'), parseInt(chalId ?? '-1')]
  const modals = useModals()
  const { classes } = useDisplayInputStyles({ fw: 'bold', ff: 'monospace' })
  const { challenge, mutate } = useEditChallenge(numId, numCId)

  const { t } = useTranslation()

  const onDeleteFlag = (flag: FlagInfoModel) => {
    modals.openConfirmModal({
      title: t('admin.button.challenges.flag.delete'),
      size: '35%',
      children: (
        <Stack>
          <Text>{t('admin.content.games.challenges.flag.delete')}</Text>
          <Input
            variant="unstyled"
            value={flag.flag}
            w="100%"
            size="md"
            readOnly
            classNames={classes}
          />
        </Stack>
      ),
      onConfirm: () => flag.id && onConfirmDeleteFlag(flag.id),
      confirmProps: { color: 'red' },
    })
  }

  const onConfirmDeleteFlag = (id: number) => {
    api.edit
      .editRemoveFlag(numId, numCId, id)
      .then(() => {
        showNotification({
          color: 'teal',
          message: t('admin.notification.games.challenges.flag.deleted'),
          icon: <Icon path={mdiCheck} size={1} />,
        })
        challenge &&
          mutate({
            ...challenge,
            flags: challenge.flags.filter((f) => f.id !== id),
          })
      })
      .catch((e) => showErrorNotification(e, t))
  }

  return (
    <WithChallengeEdit
      isLoading={!challenge}
      headProps={{ justify: 'apart' }}
      backUrl={`/admin/games/${id}/challenges`}
      head={
        <>
          <Title lineClamp={1} style={{ wordBreak: 'break-all' }}>
            # {challenge?.title}
          </Title>
          <Group wrap="nowrap" justify="right">
            <Button
              leftSection={<Icon path={mdiPuzzleEditOutline} size={1} />}
              onClick={() => navigate(`/admin/games/${id}/challenges/${numCId}`)}
            >
              {t('admin.button.challenges.edit')}
            </Button>
          </Group>
        </>
      }
    >
      {challenge && challenge.type === ChallengeType.DynamicAttachment ? (
        <FlagsWithAttachments onDelete={onDeleteFlag} />
      ) : (
        <OneAttachmentWithFlags onDelete={onDeleteFlag} />
      )}
    </WithChallengeEdit>
  )
}

export default GameChallengeEdit
