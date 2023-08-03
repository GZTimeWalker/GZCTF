import { FC, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  Button,
  Chip,
  Divider,
  FileButton,
  Group,
  Input,
  Progress,
  Stack,
  TextInput,
  Text,
  Title,
  useMantineTheme,
  ScrollArea,
  Overlay,
  Center,
  Code,
  List,
} from '@mantine/core'
import { useModals } from '@mantine/modals'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiKeyboardBackspace, mdiPuzzleEditOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import AttachmentRemoteEditModal from '@Components/admin/AttachmentRemoteEditModal'
import AttachmentUploadModal from '@Components/admin/AttachmentUploadModal'
import FlagCreateModal from '@Components/admin/FlagCreateModal'
import FlagEditPanel from '@Components/admin/FlagEditPanel'
import WithGameEditTab from '@Components/admin/WithGameEditTab'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import { useUploadStyles } from '@Utils/ThemeOverride'
import { useEditChallenge } from '@Utils/useEdit'
import api, { ChallengeType, FileType, FlagInfoModel } from '@Api'

const FileTypeDesrcMap = new Map<FileType, string>([
  [FileType.None, '无附件'],
  [FileType.Remote, '远程文件'],
  [FileType.Local, '平台附件'],
])

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
          message: '附件已更新',
          icon: <Icon path={mdiCheck} size={1} />,
        })
        setType(FileType.None)
        challenge &&
          mutate({
            ...challenge,
            attachment: null,
          })
      })
      .catch((err) => showErrorNotification(err))
      .finally(() => {
        setDisabled(false)
      })
  }

  const { classes, theme } = useUploadStyles()
  const [progress, setProgress] = useState(0)
  const [flagCreateModalOpen, setFlagCreateModalOpen] = useState(false)

  const onUpload = (file: File) => {
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
                message: '附件已更新',
                icon: <Icon path={mdiCheck} size={1} />,
              })
            })
            .catch((err) => showErrorNotification(err))
            .finally(() => {
              setDisabled(false)
            })
        }
      })
      .catch((err) => showErrorNotification(err))
      .finally(() => {
        setDisabled(false)
      })
  }

  const onRemote = () => {
    if (remoteUrl.startsWith('http')) {
      setDisabled(true)
      api.edit
        .editUpdateAttachment(numId, numCId, {
          attachmentType: FileType.Remote,
          remoteUrl: remoteUrl,
        })
        .then(() => {
          showNotification({
            color: 'teal',
            message: '附件已更新',
            icon: <Icon path={mdiCheck} size={1} />,
          })
        })
        .catch((err) => showErrorNotification(err))
        .finally(() => {
          setDisabled(false)
        })
    }
  }

  const onChangeFlagTemplate = () => {
    if (flagTemplate !== challenge?.flagTemplate) {
      setDisabled(true)
      api.edit
        // allow empty flag template to be set (but not null or undefined)
        .editUpdateGameChallenge(numId, numCId, { flagTemplate })
        .then(() => {
          showNotification({
            color: 'teal',
            message: 'flag 模板已更新',
            icon: <Icon path={mdiCheck} size={1} />,
          })
          challenge && mutate({ ...challenge, flagTemplate: flagTemplate })
        })
        .catch(showErrorNotification)
        .finally(() => {
          setDisabled(false)
        })
    }
  }

  return (
    <Stack>
      <Group position="apart">
        <Title order={2}>附件管理</Title>
        {type !== FileType.Remote ? (
          <FileButton onChange={onUpload}>
            {(props) => (
              <Button
                {...props}
                fullWidth
                className={classes.uploadButton}
                disabled={type !== FileType.Local}
                w="122px"
                mt="24px"
                color={progress !== 0 ? 'cyan' : theme.primaryColor}
              >
                <div className={classes.uploadLabel}>{progress !== 0 ? '上传中' : '上传附件'}</div>
                {progress !== 0 && (
                  <Progress
                    value={progress}
                    className={classes.uploadProgress}
                    color={theme.fn.rgba(theme.colors[theme.primaryColor][2], 0.35)}
                    radius="sm"
                  />
                )}
              </Button>
            )}
          </FileButton>
        ) : (
          <Button disabled={disabled} w="122px" mt="24px" onClick={onRemote}>
            保存链接
          </Button>
        )}
      </Group>
      <Divider />
      <Group position="apart">
        <Input.Wrapper label="附件类型" required>
          <Chip.Group
            value={type}
            onChange={(e) => {
              if (e === FileType.None) {
                modals.openConfirmModal({
                  title: '清除附件',
                  children: <Text size="sm">你确定要清除本题的附件吗？</Text>,
                  onConfirm: onConfirmClear,
                  labels: { confirm: '确认', cancel: '取消' },
                  confirmProps: { color: 'orange' },
                })
              } else {
                setType(e as FileType)
              }
            }}
          >
            <Group position="left" spacing="sm" h="2.25rem">
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
            label="附件链接"
            readOnly
            disabled={disabled || type === FileType.None}
            value={challenge?.attachment?.url ?? ''}
            w="calc(100% - 320px)"
            onClick={() => challenge?.attachment?.url && window.open(challenge?.attachment?.url)}
          />
        ) : (
          <TextInput
            label="附件链接"
            disabled={disabled}
            value={remoteUrl}
            w="calc(100% - 320px)"
            onChange={(e) => setRemoteUrl(e.target.value)}
          />
        )}
      </Group>
      <Group position="apart" mt={20}>
        <Title order={2}>flag 管理</Title>
        {challenge?.type === ChallengeType.DynamicContainer ? (
          <Button disabled={disabled} onClick={onChangeFlagTemplate}>
            保存 flag 模版
          </Button>
        ) : (
          <Button disabled={disabled} w="122px" onClick={() => setFlagCreateModalOpen(true)}>
            添加 flag
          </Button>
        )}
      </Group>
      <Divider />
      {challenge?.type === ChallengeType.DynamicContainer ? (
        <Stack>
          <TextInput
            label="flag 模板"
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
          <Stack spacing={6} pb={8}>
            <Text size="sm">请输入 flag 模版字符串，留空以生成随机 GUID 作为 flag</Text>
            <Text size="sm">
              指定 <Code>[GUID]</Code>则会仅替换此处的占位符为随机 GUID
            </Text>
            <Text size="sm">
              若指定 <Code>[TEAM_HASH]</Code> 则它将会被自动替换为队伍 Token
              与相关信息所生成的哈希值
            </Text>
            <Text size="sm">
              若未指定 <Code>[TEAM_HASH]</Code> 则将启用 Leet
              字符串功能，将会基于模版对花括号内字符串进行变换，需要确保 flag 模版字符串的熵足够高
            </Text>
            <Text size="sm">
              若需要在指定 <Code>[TEAM_HASH]</Code> 的情况下启用 Leet 字符串功能，请在 flag
              模版字符串
              <Text span fw={700}>
                之前
              </Text>
              添加 <Code>[LEET]</Code> 标记，此时不会检查 flag 模版字符串的熵
            </Text>
            <Text size="sm" fw="bold">
              flag 模板编写示例
            </Text>
            <List size="sm" spacing={6}>
              <List.Item>
                留空会得到 <Code>{`flag{1bab71b8-117f-4dea-a047-340b72101d7b}`}</Code>
              </List.Item>
              <List.Item>
                <Code>{`flag{hello world}`}</Code> 会得到 <Code>{`flag{He1lo_w0r1d}`}</Code>
              </List.Item>
              <List.Item>
                <Code>{`flag{hello_world_[TEAM_HASH]}`}</Code> 会得到{' '}
                <Code>{`flag{hello_world_5418ce4d815c}`}</Code>
              </List.Item>
              <List.Item>
                <Code>{`[LEET]flag{hello world [TEAM_HASH]}`}</Code> 会得到{' '}
                <Code>{`flag{He1lo_w0r1d_5418ce4d815c}`}</Code>
              </List.Item>
            </List>
          </Stack>
        </Stack>
      ) : (
        <ScrollArea h="calc(100vh - 430px)" pos="relative">
          {!challenge?.flags.length && (
            <>
              <Overlay opacity={0.3} color={theme.colorScheme === 'dark' ? 'black' : 'white'} />
              <Center h="calc(100vh - 430px)">
                <Stack spacing={0}>
                  <Title order={2}>flag 列表为空</Title>
                  <Text>请通过右上角添加 flag</Text>
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
        title="添加 flag"
        opened={flagCreateModalOpen}
        onClose={() => setFlagCreateModalOpen(false)}
      />
    </Stack>
  )
}

const FlagsWithAttachments: FC<FlagEditProps> = ({ onDelete }) => {
  const { id, chalId } = useParams()
  const [numId, numCId] = [parseInt(id ?? '-1'), parseInt(chalId ?? '-1')]

  const theme = useMantineTheme()

  const { challenge } = useEditChallenge(numId, numCId)

  const [attachmentUploadModalOpened, setAttachmentUploadModalOpened] = useState(false)
  const [remoteAttachmentModalOpened, setRemoteAttachmentModalOpened] = useState(false)

  return (
    <Stack>
      <Group position="apart" mt={20}>
        <Title order={2}>flag 管理</Title>
        <Group position="right">
          <Button onClick={() => setRemoteAttachmentModalOpened(true)}>添加远程附件</Button>
          <Button onClick={() => setAttachmentUploadModalOpened(true)}>上传动态附件</Button>
        </Group>
      </Group>
      <Divider />
      <ScrollArea h="calc(100vh - 250px)" pos="relative">
        {!challenge?.flags.length && (
          <>
            <Overlay opacity={0.3} color={theme.colorScheme === 'dark' ? 'black' : 'white'} />
            <Center h="calc(100vh - 250px)">
              <Stack spacing={0}>
                <Title order={2}>flag 列表为空</Title>
                <Text>请通过右上角添加 flag</Text>
              </Stack>
            </Center>
          </>
        )}
        <FlagEditPanel flags={challenge?.flags} onDelete={onDelete} />
      </ScrollArea>
      <AttachmentUploadModal
        title="批量添加动态附件"
        size="40%"
        opened={attachmentUploadModalOpened}
        onClose={() => setAttachmentUploadModalOpened(false)}
      />
      <AttachmentRemoteEditModal
        title="批量添加远程附件"
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

  const theme = useMantineTheme()
  const modals = useModals()

  const { challenge, mutate } = useEditChallenge(numId, numCId)

  const onDeleteFlag = (flag: FlagInfoModel) => {
    modals.openConfirmModal({
      title: '删除 flag',
      size: '35%',
      children: (
        <Stack>
          <Text>确定删除下列 flag 吗？</Text>
          <Text ff={theme.fontFamilyMonospace}>{flag.flag}</Text>
        </Stack>
      ),
      onConfirm: () => flag.id && onConfirmDeleteFlag(flag.id),

      labels: { confirm: '确认', cancel: '取消' },
      confirmProps: { color: 'red' },
    })
  }

  const onConfirmDeleteFlag = (id: number) => {
    api.edit
      .editRemoveFlag(numId, numCId, id)
      .then(() => {
        showNotification({
          color: 'teal',
          message: 'flag 已删除',
          icon: <Icon path={mdiCheck} size={1} />,
        })
        challenge &&
          mutate({
            ...challenge,
            flags: challenge.flags.filter((f) => f.id !== id),
          })
      })
      .catch(showErrorNotification)
  }

  return (
    <WithGameEditTab
      isLoading={!challenge}
      headProps={{ position: 'apart' }}
      head={
        <>
          <Group noWrap position="left">
            <Button
              leftIcon={<Icon path={mdiKeyboardBackspace} size={1} />}
              onClick={() => navigate(`/admin/games/${id}/challenges`)}
            >
              返回上级
            </Button>
            <Title lineClamp={1} style={{ wordBreak: 'break-all' }}>
              # {challenge?.title}
            </Title>
          </Group>
          <Group noWrap position="right">
            <Button
              leftIcon={<Icon path={mdiPuzzleEditOutline} size={1} />}
              onClick={() => navigate(`/admin/games/${id}/challenges/${numCId}`)}
            >
              编辑题目信息
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
    </WithGameEditTab>
  )
}

export default GameChallengeEdit
