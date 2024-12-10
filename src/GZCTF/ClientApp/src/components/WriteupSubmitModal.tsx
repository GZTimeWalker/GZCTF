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
  alpha,
  useMantineTheme,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiExclamationThick, mdiFileDocumentOutline, mdiFileHidden } from '@mdi/js'
import { Icon } from '@mdi/react'
import cx from 'clsx'
import dayjs from 'dayjs'
import { FC, useEffect, useState } from 'react'
import { Trans, useTranslation } from 'react-i18next'
import { Markdown } from '@Components/MarkdownRenderer'
import { showErrorNotification } from '@Utils/ApiHelper'
import { useLanguage } from '@Utils/I18n'
import { HunamizeSize } from '@Utils/Shared'
import { OnceSWRConfig } from '@Hooks/useConfig'
import api from '@Api'
import misc from '@Styles/Misc.module.css'
import uploadClasses from '@Styles/Upload.module.css'

interface WriteupSubmitModalProps extends ModalProps {
  gameId: number
  writeupDeadline: number
}

export const WriteupSubmitModal: FC<WriteupSubmitModalProps> = ({ gameId, writeupDeadline: wpddl, ...props }) => {
  const { data, mutate } = api.game.useGameGetWriteup(gameId, OnceSWRConfig)

  const theme = useMantineTheme()
  const [ddl, setDdl] = useState(dayjs(wpddl))
  const { locale } = useLanguage()
  const [disabled, setDisabled] = useState(dayjs().isAfter(wpddl))
  const [progress, setProgress] = useState(0)
  const noteColor = data?.submitted ? theme.colors.teal[5] : theme.colors.red[5]

  const { t } = useTranslation()

  useEffect(() => {
    setDdl(dayjs(wpddl))
    setDisabled(dayjs().isAfter(wpddl))
  }, [wpddl])

  const onUpload = async (file: File | null) => {
    if (!file || disabled) return

    setProgress(0)
    setDisabled(true)

    try {
      await api.game.gameSubmitWriteup(
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
      setProgress(100)
      showNotification({
        color: 'teal',
        message: t('game.notification.writeup.submitted'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
      mutate()
      setDisabled(false)
    } catch (err) {
      showErrorNotification(err, t)
    } finally {
      setProgress(0)
      setDisabled(false)
    }
  }

  return (
    <Modal
      title={
        <Group w="100%" justify="space-between">
          <Title order={4}>{t('game.content.writeup.title')}</Title>
          <Group gap={4}>
            <Icon path={data?.submitted ? mdiCheck : mdiExclamationThick} size={0.9} color={noteColor} />
            <Text fw={600} size="md" c={noteColor}>
              {data?.submitted ? t('game.content.writeup.submitted') : t('game.content.writeup.unsubmitted')}
            </Text>
          </Group>
        </Group>
      }
      {...props}
      classNames={{
        header: misc.m0,
        title: cx(misc.w100, misc.m0),
      }}
    >
      <Stack gap="xs" mt={0}>
        <Divider />
        <Title order={5}>{t('game.content.writeup.instructions.title')}</Title>
        <List classNames={{ itemWrapper: misc.listItemWrapper }}>
          <List.Item>
            <Text>
              <Trans
                i18nKey="game.content.writeup.instructions.deadline"
                values={{
                  datetime: ddl.locale(locale).format('LL LTS'),
                }}
              >
                _
                <Text mx={5} span fw={600} c="yellow">
                  _
                </Text>
                _
              </Trans>
            </Text>
          </List.Item>
          <List.Item>
            <Text>
              <Trans i18nKey="game.content.writeup.instructions.file_format">
                _
                <Text mx={5} fw={600} span c="yellow">
                  _
                </Text>
                _
              </Trans>
            </Text>
          </List.Item>
        </List>
        {data?.note && (
          <>
            <Title order={5}>{t('game.content.writeup.instructions.additional')}</Title>
            <Markdown source={data.note} />
          </>
        )}
        <Title order={5}>{t('game.content.writeup.current')}</Title>
        <Card>
          {data && data.submitted ? (
            <Group>
              <Icon path={mdiFileDocumentOutline} size={1.5} />
              <Stack gap={0}>
                <Text fw={600} size="md">
                  {data.name}
                </Text>
                <Text fw={600} size="sm" c="dimmed" ff="monospace">
                  {data.fileSize && HunamizeSize(data.fileSize)}
                </Text>
              </Stack>
            </Group>
          ) : (
            <Group>
              <Icon path={mdiFileHidden} size={1.5} />
              <Stack gap={0}>
                <Text fw={600} size="md">
                  {t('game.content.writeup.unsubmitted_note')}
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
              className={uploadClasses.button}
              disabled={disabled}
              color={progress !== 0 ? 'cyan' : theme.primaryColor}
            >
              <div className={uploadClasses.label}>
                {dayjs().isAfter(ddl)
                  ? t('game.content.writeup.deadline_exceeded')
                  : progress !== 0
                    ? t('game.button.writeup.uploading')
                    : t('game.button.writeup.upload')}
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
      </Stack>
    </Modal>
  )
}
