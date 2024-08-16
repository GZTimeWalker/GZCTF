import {
  ActionIcon,
  Button,
  Center,
  Grid,
  Group,
  Image,
  Input,
  NumberInput,
  SimpleGrid,
  Stack,
  Switch,
  TagsInput,
  Text,
  Textarea,
  TextInput,
} from '@mantine/core'
import { DatePickerInput, TimeInput } from '@mantine/dates'
import { Dropzone } from '@mantine/dropzone'
import { useClipboard, useInputState } from '@mantine/hooks'
import { useModals } from '@mantine/modals'
import { notifications, showNotification, updateNotification } from '@mantine/notifications'
import {
  mdiCheck,
  mdiClipboard,
  mdiClose,
  mdiContentSaveOutline,
  mdiDeleteOutline,
  mdiRefresh,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate, useParams } from 'react-router-dom'
import { SwitchLabel } from '@Components/admin/SwitchLabel'
import WithGameEditTab from '@Components/admin/WithGameEditTab'
import { showErrorNotification, tryGetErrorMsg } from '@Utils/ApiHelper'
import { IMAGE_MIME_TYPES } from '@Utils/Shared'
import { OnceSWRConfig } from '@Utils/useConfig'
import api, { GameInfoModel } from '@Api'

const GenerateRandomCode = () => {
  const chars = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789'
  let code = ''
  for (let i = 0; i < 24; i++) {
    code += chars[Math.floor(Math.random() * chars.length)]
  }
  return code
}

const GameInfoEdit: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const { data: gameSource, mutate } = api.edit.useEditGetGame(numId, OnceSWRConfig)
  const [game, setGame] = useState<GameInfoModel>()
  const navigate = useNavigate()

  const [disabled, setDisabled] = useState(false)
  const [start, setStart] = useInputState(dayjs())
  const [end, setEnd] = useInputState(dayjs())
  const [wpddl, setWpddl] = useInputState(3)

  const modals = useModals()
  const clipboard = useClipboard()

  const { t } = useTranslation()

  useEffect(() => {
    if (numId < 0) {
      showNotification({
        color: 'red',
        message: t('common.error.param_error'),
        icon: <Icon path={mdiClose} size={1} />,
      })
      navigate('/admin/games')
      return
    }

    if (gameSource) {
      setGame(gameSource)
      setStart(dayjs(gameSource.start))
      setEnd(dayjs(gameSource.end))

      const wpddl = dayjs(gameSource.writeupDeadline).diff(gameSource.end, 'h')
      setWpddl(wpddl < 0 ? 0 : wpddl)
    }
  }, [id, gameSource])

  const onUpdatePoster = (file: File | undefined) => {
    if (!game || !file) return

    setDisabled(true)
    notifications.clean()
    showNotification({
      id: 'upload-poster',
      color: 'orange',
      message: t('admin.notification.games.info.poster.uploading'),
      loading: true,
      autoClose: false,
    })

    api.edit
      .editUpdateGamePoster(game.id!, { file })
      .then((res) => {
        updateNotification({
          id: 'upload-poster',
          color: 'teal',
          message: t('admin.notification.games.info.poster.uploaded'),
          icon: <Icon path={mdiCheck} size={1} />,
          autoClose: true,
          loading: false,
        })
        mutate({ ...game, poster: res.data })
      })
      .catch((err) => {
        updateNotification({
          id: 'upload-poster',
          color: 'red',
          title: t('admin.notification.games.info.poster.upload_failed'),
          message: tryGetErrorMsg(err, t),
          icon: <Icon path={mdiClose} size={1} />,
          autoClose: true,
          loading: false,
        })
      })
      .finally(() => {
        setDisabled(false)
      })
  }

  const onUpdateInfo = () => {
    if (!game?.title) return

    setDisabled(true)
    api.edit
      .editUpdateGame(game.id!, {
        ...game,
        inviteCode: (game.inviteCode?.length ?? 0 > 6) ? game.inviteCode : null,
        start: start.toJSON(),
        end: end.toJSON(),
        writeupDeadline: end.add(wpddl, 'h').toJSON(),
      })
      .then(() => {
        showNotification({
          color: 'teal',
          message: t('admin.notification.games.info.info_updated'),
          icon: <Icon path={mdiCheck} size={1} />,
        })
        mutate()
        api.game.mutateGameGamesAll()
      })
      .catch((e) => showErrorNotification(e, t))
      .finally(() => {
        setDisabled(false)
      })
  }

  const onConfirmDelete = () => {
    if (!game) return
    api.edit
      .editDeleteGame(game.id!)
      .then(() => {
        showNotification({
          color: 'teal',
          message: t('admin.notification.games.info.deleted'),
          icon: <Icon path={mdiCheck} size={1} />,
        })
        navigate('/admin/games')
      })
      .catch((e) => showErrorNotification(e, t))
  }

  const onCopyPublicKey = () => {
    clipboard.copy(game?.publicKey || '')
    showNotification({
      color: 'teal',
      message: t('admin.notification.games.info.public_key_copied'),
      icon: <Icon path={mdiCheck} size={1} />,
    })
  }

  return (
    <WithGameEditTab
      headProps={{ justify: 'apart' }}
      contentPos="right"
      isLoading={!game}
      head={
        <>
          <Button
            disabled={disabled}
            color="red"
            leftSection={<Icon path={mdiDeleteOutline} size={1} />}
            variant="outline"
            onClick={() =>
              modals.openConfirmModal({
                title: t('admin.button.games.delete'),
                children: (
                  <Text size="sm">
                    {t('admin.content.games.info.delete', { name: game?.title })}
                  </Text>
                ),
                onConfirm: () => onConfirmDelete(),
                confirmProps: { color: 'red' },
              })
            }
          >
            {t('admin.button.games.delete')}
          </Button>
          <Button
            leftSection={<Icon path={mdiClipboard} size={1} />}
            disabled={disabled}
            onClick={onCopyPublicKey}
          >
            {t('admin.button.games.copy_public_key')}
          </Button>
          <Button
            leftSection={<Icon path={mdiContentSaveOutline} size={1} />}
            disabled={disabled}
            onClick={onUpdateInfo}
          >
            {t('admin.button.save')}
          </Button>
        </>
      }
    >
      <SimpleGrid cols={4}>
        <TextInput
          label={t('admin.content.games.info.title.label')}
          description={t('admin.content.games.info.title.description')}
          disabled={disabled}
          value={game?.title}
          required
          onChange={(e) => game && setGame({ ...game, title: e.target.value })}
        />
        <NumberInput
          label={t('admin.content.games.info.member_limit.label')}
          description={t('admin.content.games.info.member_limit.description')}
          disabled={disabled}
          min={0}
          required
          value={game?.teamMemberCountLimit}
          onChange={(e) => game && setGame({ ...game, teamMemberCountLimit: Number(e) })}
        />
        <NumberInput
          label={t('admin.content.games.info.container_limit.label')}
          description={t('admin.content.games.info.container_limit.description')}
          disabled={disabled}
          min={0}
          required
          value={game?.containerCountLimit}
          onChange={(e) => game && setGame({ ...game, containerCountLimit: Number(e) })}
        />

        <TextInput
          label={t('admin.content.games.info.invite_code.label')}
          description={t('admin.content.games.info.invite_code.description')}
          value={game?.inviteCode || ''}
          disabled={disabled}
          onChange={(e) => game && setGame({ ...game, inviteCode: e.target.value })}
          rightSection={
            <ActionIcon
              onClick={() => game && setGame({ ...game, inviteCode: GenerateRandomCode() })}
            >
              <Icon path={mdiRefresh} size={1} />
            </ActionIcon>
          }
        />
        <DatePickerInput
          label={t('admin.content.games.info.start_date')}
          value={start.toDate()}
          disabled={disabled}
          clearable={false}
          onChange={(e) => {
            const newDate = dayjs(e)
              .hour(start.hour())
              .minute(start.minute())
              .second(start.second())
            setStart(newDate)
            if (newDate && end < newDate) {
              setEnd(newDate.add(2, 'h'))
            }
          }}
          required
        />
        <TimeInput
          label={t('admin.content.games.info.start_time')}
          disabled={disabled}
          value={start.format('HH:mm:ss')}
          onChange={(e) => {
            const newTime = e.target.value.split(':')
            const newDate = dayjs(start)
              .hour(Number(newTime[0]))
              .minute(Number(newTime[1]))
              .second(Number(newTime[2]))
              .millisecond(0)
            setStart(newDate)
            if (newDate && end < newDate) {
              setEnd(newDate.add(2, 'h'))
            }
          }}
          withSeconds
          required
        />
        <DatePickerInput
          label={t('admin.content.games.info.end_date')}
          disabled={disabled}
          minDate={start.toDate()}
          value={end.toDate()}
          clearable={false}
          onChange={(e) => {
            const newDate = dayjs(e).hour(end.hour()).minute(end.minute()).second(end.second())
            setEnd(newDate)
          }}
          error={end < start}
          required
        />
        <TimeInput
          label={t('admin.content.games.info.end_time')}
          disabled={disabled}
          value={end.format('HH:mm:ss')}
          onChange={(e) => {
            const newTime = e.target.value.split(':')
            const newDate = dayjs(end)
              .hour(Number(newTime[0]))
              .minute(Number(newTime[1]))
              .second(Number(newTime[2]))
              .millisecond(0)
            setEnd(newDate)
          }}
          error={end < start}
          withSeconds
          required
        />
      </SimpleGrid>
      <Grid>
        <Grid.Col span={6}>
          <Textarea
            label={t('admin.content.games.info.summary.label')}
            description={t('admin.content.games.info.summary.description')}
            value={game?.summary}
            w="100%"
            autosize
            disabled={disabled}
            minRows={3}
            maxRows={3}
            onChange={(e) => game && setGame({ ...game, summary: e.target.value })}
          />
        </Grid.Col>
        <Grid.Col span={3}>
          <Stack gap="xs" h="100%" justify="space-between">
            <Switch
              pt="1.5em"
              disabled={disabled}
              checked={game?.writeupRequired ?? false}
              label={SwitchLabel(
                t('admin.content.games.info.writeup_required.label'),
                t('admin.content.games.info.writeup_required.description')
              )}
              onChange={(e) => game && setGame({ ...game, writeupRequired: e.target.checked })}
            />
            <Switch
              disabled={disabled}
              checked={game?.acceptWithoutReview ?? false}
              label={SwitchLabel(
                t('admin.content.games.info.accept_without_review.label'),
                t('admin.content.games.info.accept_without_review.description')
              )}
              onChange={(e) => game && setGame({ ...game, acceptWithoutReview: e.target.checked })}
            />
          </Stack>
        </Grid.Col>
        <Grid.Col span={3}>
          <Stack gap="xs">
            <NumberInput
              label={t('admin.content.games.info.writeup_deadline.label')}
              description={t('admin.content.games.info.writeup_deadline.description')}
              disabled={disabled}
              min={0}
              required
              value={wpddl}
              onChange={(e) => setWpddl(Number(e))}
            />
            <Switch
              disabled={disabled}
              checked={game?.practiceMode ?? true}
              label={SwitchLabel(
                t('admin.content.games.info.practice_mode.label'),
                t('admin.content.games.info.practice_mode.description')
              )}
              onChange={(e) => game && setGame({ ...game, practiceMode: e.target.checked })}
            />
          </Stack>
        </Grid.Col>
      </Grid>
      <Group grow justify="space-between">
        <Textarea
          label={
            <Group gap="sm">
              <Text size="sm">{t('admin.content.games.info.writeup_instruction')}</Text>
              <Text size="xs" c="dimmed">
                {t('admin.content.markdown_support')}
              </Text>
            </Group>
          }
          value={game?.writeupNote}
          w="100%"
          autosize
          disabled={disabled}
          minRows={3}
          maxRows={3}
          onChange={(e) => game && setGame({ ...game, writeupNote: e.target.value })}
        />
        <TagsInput
          label={
            <Group gap="sm">
              <Text size="sm"> {t('admin.content.games.info.organizations.label')}</Text>
              <Text size="xs" c="dimmed">
                {t('admin.content.games.info.organizations.description')}
              </Text>
            </Group>
          }
          disabled={disabled}
          placeholder={t('admin.placeholder.games.organizations')}
          maxDropdownHeight={300}
          value={game?.organizations ?? []}
          styles={{
            input: {
              minHeight: 79,
              maxHeight: 79,
              overflow: 'auto',
            },
          }}
          onChange={(e) => game && setGame({ ...game, organizations: e })}
          onClear={() => game && setGame({ ...game, organizations: [] })}
        />
      </Group>
      <Grid grow>
        <Grid.Col span={8}>
          <Textarea
            label={
              <Group gap="sm">
                <Text size="sm">{t('admin.content.games.info.content')}</Text>
                <Text size="xs" c="dimmed">
                  {t('admin.content.markdown_support')}
                </Text>
              </Group>
            }
            value={game?.content}
            w="100%"
            autosize
            disabled={disabled}
            minRows={9}
            maxRows={9}
            onChange={(e) => game && setGame({ ...game, content: e.target.value })}
          />
        </Grid.Col>
        <Grid.Col span={4}>
          <Input.Wrapper label={t('admin.content.games.info.poster')}>
            <Dropzone
              onDrop={(files) => onUpdatePoster(files[0])}
              onReject={() => {
                showNotification({
                  color: 'red',
                  title: t('common.error.file_invalid.title'),
                  message: t('common.error.file_invalid.message'),
                  icon: <Icon path={mdiClose} size={1} />,
                })
              }}
              maxSize={3 * 1024 * 1024}
              accept={IMAGE_MIME_TYPES}
              disabled={disabled}
              styles={{
                root: {
                  height: '211px',
                  padding: game?.poster ? '0' : '16px',
                },
              }}
            >
              <Center style={{ pointerEvents: 'none' }}>
                {game?.poster ? (
                  <Image height="209px" fit="contain" src={game.poster} alt="poster" />
                ) : (
                  <Center h="200px">
                    <Stack gap={0}>
                      <Text size="xl" inline>
                        {t('common.content.drop_zone.content', {
                          type: t('common.content.drop_zone.type.poster'),
                        })}
                      </Text>
                      <Text size="sm" c="dimmed" inline mt={7}>
                        {t('common.content.drop_zone.limit')}
                      </Text>
                    </Stack>
                  </Center>
                )}
              </Center>
            </Dropzone>
          </Input.Wrapper>
        </Grid.Col>
      </Grid>
    </WithGameEditTab>
  )
}

export default GameInfoEdit
