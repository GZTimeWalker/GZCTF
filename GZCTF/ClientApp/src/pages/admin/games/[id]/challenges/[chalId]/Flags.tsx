import { FC, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  Button,
  Chip,
  createStyles,
  FileButton,
  Group,
  Input,
  Progress,
  Stack,
  TextInput,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiBackburger, mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import api, { ChallengeType, FileType } from '../../../../../../Api'
import WithGameTab from '../../../../../../components/admin/WithGameTab'

const FileTypeDesrcMap = new Map<FileType, string>([
  [FileType.None, '无附件'],
  [FileType.Remote, '远程文件'],
  [FileType.Local, '平台附件'],
])

const useStyles = createStyles(() => ({
  uploadButton: {
    position: 'relative',
    transition: 'background-color 150ms ease',
  },

  uploadProgress: {
    position: 'absolute',
    bottom: -1,
    right: -1,
    left: -1,
    top: -1,
    height: 'auto',
    backgroundColor: 'transparent',
    zIndex: 0,
  },

  uploadLabel: {
    position: 'relative',
    zIndex: 1,
  },
}))

// with only one attachment
const OneAttachmentWithFlags: FC = () => {
  const { id, chalId } = useParams()
  const [numId, numCId] = [parseInt(id ?? '-1'), parseInt(chalId ?? '-1')]

  const { data: challenge, mutate } = api.edit.useEditGetGameChallenge(numId, numCId, {
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  })

  const [disabled, setDisabled] = useState(false)
  const [type, setType] = useState<FileType>(challenge?.attachment?.type ?? FileType.None)

  useEffect(() => {
    if (challenge) {
      setType(challenge.attachment?.type ?? FileType.None)
    }
  }, [challenge])

  const { classes, theme } = useStyles()
  const [progress, setProgress] = useState(0)

  const [remoteUrl, setRemoteUrl] = useState(challenge?.attachment?.remoteUrl ?? '')

  const onUpload = (file: File) => {
    setProgress(0)
    setDisabled(true)

    api.assets
      .assetsUpload(
        {
          files: [file],
        },
        {
          onUploadProgress: (e) => {
            setProgress((e.loaded / e.total) * 100)
          },
        }
      )
      .then((data) => {
        const file = data.data[0]
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
                disallowClose: true,
              })
            })
            .catch((err) =>
              showNotification({
                color: 'red',
                title: '遇到了问题',
                message: `${err.error.title}`,
                icon: <Icon path={mdiClose} size={1} />,
                disallowClose: true,
              })
            )
            .finally(() => {
              setDisabled(false)
            })
        }
      })
      .catch((err) =>
        showNotification({
          color: 'red',
          title: '遇到了问题',
          message: `${err.error.title}`,
          icon: <Icon path={mdiClose} size={1} />,
          disallowClose: true,
        })
      )
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
            disallowClose: true,
          })
        })
        .catch((err) =>
          showNotification({
            color: 'red',
            title: '遇到了问题',
            message: `${err.error.title}`,
            icon: <Icon path={mdiClose} size={1} />,
            disallowClose: true,
          })
        )
        .finally(() => {
          setDisabled(false)
        })
    }
  }

  return (
    <Stack>
      <Group position="apart">
        <Input.Wrapper label="附件类型" required>
          <Chip.Group mt={8} value={type} onChange={(e) => setType(e as FileType)}>
            {Object.entries(FileType).map((type) => (
              <Chip key={type[0]} value={type[1]}>
                {FileTypeDesrcMap.get(type[1])}
              </Chip>
            ))}
          </Chip.Group>
        </Input.Wrapper>
        <Group position="right" style={{ width: 'calc(100% - 20rem)' }}>
          {type !== FileType.Remote ? (
            <>
              <TextInput
                label="附件链接"
                readOnly
                disabled={disabled || type === FileType.None}
                value={challenge?.attachment?.url ?? ''}
                style={{ width: 'calc(100% - 142px)' }}
                onClick={() =>
                  challenge?.attachment?.url && window.open(challenge?.attachment?.url)
                }
              />
              <FileButton onChange={onUpload}>
                {(props) => (
                  <Button
                    {...props}
                    fullWidth
                    className={classes.uploadButton}
                    disabled={type !== FileType.Local}
                    style={{ width: '122px', marginTop: '24px' }}
                    color={progress !== 0 ? 'cyan' : theme.primaryColor}
                  >
                    <div className={classes.uploadLabel}>
                      {progress !== 0 ? '上传中' : '上传附件'}
                    </div>
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
            </>
          ) : (
            <>
              <TextInput
                label="附件链接"
                disabled={disabled}
                value={remoteUrl}
                style={{ width: 'calc(100% - 142px)' }}
                onChange={(e) => setRemoteUrl(e.target.value)}
              />
              <Button disabled={disabled} style={{ width: '122px', marginTop: '24px' }} onClick={onRemote}>
                保存链接
              </Button>
            </>
          )}
        </Group>
      </Group>
      <Group position="right"></Group>
    </Stack>
  )
}

const FlagsWithAttachments: FC = () => {
  return (
    <Stack>
      <Group position="apart">
        <Stack spacing="xs"></Stack>
      </Group>
    </Stack>
  )
}

const GameChallengeEdit: FC = () => {
  const navigate = useNavigate()
  const { id, chalId } = useParams()
  const [numId, numCId] = [parseInt(id ?? '-1'), parseInt(chalId ?? '-1')]

  const { data: challenge } = api.edit.useEditGetGameChallenge(numId, numCId, {
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  })

  return (
    <WithGameTab
      isLoading={!challenge}
      headProps={{ position: 'apart' }}
      head={
        <>
          <Button
            leftIcon={<Icon path={mdiBackburger} size={1} />}
            onClick={() => navigate(`/admin/games/${id}/challenges`)}
          >
            返回上级
          </Button>
          <Group position="right">
            <Button onClick={() => navigate(`/admin/games/${id}/challenges/${numCId}`)}>
              编辑题目信息
            </Button>
          </Group>
        </>
      }
    >
      {challenge && challenge.type === ChallengeType.DynamicAttachment ? (
        <FlagsWithAttachments />
      ) : (
        <OneAttachmentWithFlags />
      )}
    </WithGameTab>
  )
}

export default GameChallengeEdit
