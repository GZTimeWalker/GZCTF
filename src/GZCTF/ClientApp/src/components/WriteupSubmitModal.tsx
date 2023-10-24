import dayjs from 'dayjs'
import { FC, useEffect, useState } from 'react'
import {
  Button,
  Card,
  Divider,
  FileButton,
  Group,
  List,
  Modal,
  ModalProps,
  Progress,
  Stack,
  Text,
  Title,
  useMantineTheme,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiExclamationThick, mdiFileDocumentOutline, mdiFileHidden } from '@mdi/js'
import { Icon } from '@mdi/react'
import MarkdownRender from '@Components/MarkdownRender'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import { HunamizeSize } from '@Utils/Shared'
import { useUploadStyles } from '@Utils/ThemeOverride'
import { OnceSWRConfig } from '@Utils/useConfig'
import api from '@Api'

interface WriteupSubmitModalProps extends ModalProps {
  gameId: number
  wpddl: string
}

export const WriteupSubmitModal: FC<WriteupSubmitModalProps> = ({ gameId, wpddl, ...props }) => {
  const { data, mutate } = api.game.useGameGetWriteup(gameId, OnceSWRConfig)

  const theme = useMantineTheme()
  const { classes } = useUploadStyles()
  const [ddl, setDdl] = useState(dayjs(wpddl))
  const [disabled, setDisabled] = useState(dayjs().isAfter(wpddl))
  const [progress, setProgress] = useState(0)
  const noteColor = data?.submitted ? theme.colors.teal[5] : theme.colors.red[5]

  useEffect(() => {
    setDdl(dayjs(wpddl))
    setDisabled(dayjs().isAfter(wpddl))
  }, [wpddl])

  const onUpload = (file: File) => {
    setProgress(0)
    setDisabled(true)

    api.game
      .gameSubmitWriteup(
        gameId,
        {
          file,
        },
        {
          onUploadProgress: (e) => {
            setProgress((e.loaded / (e.total ?? 1)) * 100)
          },
        }
      )
      .then(() => {
        setProgress(100)
        showNotification({
          color: 'teal',
          message: 'Writeup 已成功提交',
          icon: <Icon path={mdiCheck} size={1} />,
        })
        mutate()
        setDisabled(false)
      })
      .catch((err) => showErrorNotification(err))
      .finally(() => {
        setProgress(0)
        setDisabled(false)
      })
  }

  return (
    <Modal
      title={
        <Group w="100%" position="apart">
          <Title order={4}>管理 Writeup</Title>
          <Group spacing={4}>
            <Icon
              path={data?.submitted ? mdiCheck : mdiExclamationThick}
              size={0.9}
              color={noteColor}
            />
            <Text fw={600} size="md" c={noteColor}>
              {data?.submitted ? '已成功提交' : '尚未提交'}
            </Text>
          </Group>
        </Group>
      }
      {...props}
      styles={{
        ...props.styles,
        header: {
          margin: 0,
        },
        title: {
          width: '100%',
          margin: 0,
        },
      }}
    >
      <Stack spacing="xs" mt={0}>
        <Divider />
        <Title order={5}>提交说明</Title>
        <List
          styles={{
            itemWrapper: {
              maxWidth: 'calc(100% - 2rem)',
            },
          }}
        >
          <List.Item>
            <Text>
              请在
              <Text mx={5} span fw={600} c="yellow">
                {ddl.format(' YYYY 年 MM 月 DD 日 HH:mm:ss ')}
              </Text>
              前提交 Writeup，逾期提交或不提交视为放弃本次参赛记录。
            </Text>
          </List.Item>
          <List.Item>
            <Text>
              请将全部解出题目整理为
              <Text mx={5} fw={600} span c="yellow">
                一份小于 20MB 的标准 PDF 文档
              </Text>
            </Text>
          </List.Item>
        </List>
        {data?.note && (
          <>
            <Title order={5}>附加说明</Title>
            <MarkdownRender source={data.note} />
          </>
        )}
        <Title order={5}>当前提交</Title>
        <Card>
          {data && data.submitted ? (
            <Group>
              <Icon path={mdiFileDocumentOutline} size={1.5} />
              <Stack spacing={0}>
                <Text fw={600} size="md">
                  {data.name}
                </Text>
                <Text fw={600} size="sm" c="dimmed" ff={theme.fontFamilyMonospace}>
                  {data.fileSize && HunamizeSize(data.fileSize)}
                </Text>
              </Stack>
            </Group>
          ) : (
            <Group>
              <Icon path={mdiFileHidden} size={1.5} />
              <Stack spacing={0}>
                <Text fw={600} size="md">
                  你的队伍尚未提交 Writeup
                </Text>
              </Stack>
            </Group>
          )}
        </Card>
        <FileButton onChange={onUpload} accept="application/pdf">
          {(props) => (
            <Button
              {...props}
              fullWidth
              className={classes.uploadButton}
              disabled={disabled}
              color={progress !== 0 ? 'cyan' : theme.primaryColor}
            >
              <div className={classes.uploadLabel}>
                {dayjs().isAfter(ddl)
                  ? '提交截止时间已过'
                  : progress !== 0
                  ? '上传中'
                  : '上传 Writeup'}
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
      </Stack>
    </Modal>
  )
}

export default WriteupSubmitModal
