import dayjs from 'dayjs'
import { FC, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  Button,
  Center,
  Grid,
  Group,
  Input,
  NumberInput,
  Stack,
  Textarea,
  TextInput,
  Image,
  Text,
  MultiSelect,
  ActionIcon,
  Switch,
  PasswordInput,
  SimpleGrid,
} from '@mantine/core'
import { DatePickerInput, TimeInput } from '@mantine/dates'
import { Dropzone } from '@mantine/dropzone'
import { useClipboard, useInputState } from '@mantine/hooks'
import { useModals } from '@mantine/modals'
import { notifications, showNotification, updateNotification } from '@mantine/notifications'
import {
  mdiKeyboardBackspace,
  mdiCheck,
  mdiClose,
  mdiContentSaveOutline,
  mdiRefresh,
  mdiDeleteOutline,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import { SwitchLabel } from '@Components/admin/SwitchLabel'
import WithGameEditTab from '@Components/admin/WithGameEditTab'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import { ACCEPT_IMAGE_MIME_TYPE } from '@Utils/ThemeOverride'
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
  const [organizations, setOrganizations] = useState<string[]>([])
  const [start, setStart] = useInputState(dayjs())
  const [end, setEnd] = useInputState(dayjs())
  const [wpddl, setWpddl] = useInputState(3)

  const modals = useModals()
  const clipboard = useClipboard()

  useEffect(() => {
    if (numId < 0) {
      showNotification({
        color: 'red',
        message: `比赛 Id 错误：${id}`,
        icon: <Icon path={mdiClose} size={1} />,
      })
      navigate('/admin/games')
      return
    }

    if (gameSource) {
      setGame(gameSource)
      setStart(dayjs(gameSource.start))
      setEnd(dayjs(gameSource.end))
      setOrganizations(gameSource.organizations || [])

      const wpddl = dayjs(gameSource.wpddl).diff(gameSource.end, 'h')
      setWpddl(wpddl < 0 ? 0 : wpddl)
    }
  }, [id, gameSource])

  const onUpdatePoster = (file: File | undefined) => {
    if (game && file) {
      setDisabled(true)
      notifications.clean()
      showNotification({
        id: 'upload-poster',
        color: 'orange',
        message: '正在上传海报',
        loading: true,
        autoClose: false,
      })

      api.edit
        .editUpdateGamePoster(game.id!, { file })
        .then((res) => {
          updateNotification({
            id: 'upload-poster',
            color: 'teal',
            message: '比赛海报已更新',
            icon: <Icon path={mdiCheck} size={1} />,
            autoClose: true,
          })
          mutate({ ...game, poster: res.data })
        })
        .catch(() => {
          updateNotification({
            id: 'upload-poster',
            color: 'red',
            message: '比赛海报更新失败',
            icon: <Icon path={mdiClose} size={1} />,
            autoClose: true,
          })
        })
        .finally(() => {
          setDisabled(false)
        })
    }
  }

  const onUpdateInfo = () => {
    if (game && game.title) {
      setDisabled(true)
      api.edit
        .editUpdateGame(game.id!, {
          ...game,
          inviteCode: game.inviteCode?.length ?? 0 > 6 ? game.inviteCode : null,
          start: start.toJSON(),
          end: end.toJSON(),
          wpddl: end.add(wpddl, 'h').toJSON(),
        })
        .then(() => {
          showNotification({
            color: 'teal',
            message: '比赛信息已更新',
            icon: <Icon path={mdiCheck} size={1} />,
          })
          mutate()
          api.game.mutateGameGamesAll()
        })
        .catch(showErrorNotification)
        .finally(() => {
          setDisabled(false)
        })
    }
  }

  const onConfirmDelete = () => {
    if (game) {
      api.edit
        .editDeleteGame(game.id!)
        .then(() => {
          showNotification({
            color: 'teal',
            message: '比赛已删除',
            icon: <Icon path={mdiCheck} size={1} />,
          })
          navigate('/admin/games')
        })
        .catch(showErrorNotification)
    }
  }

  return (
    <WithGameEditTab
      headProps={{ position: 'apart' }}
      isLoading={!game}
      head={
        <>
          <Button
            leftIcon={<Icon path={mdiKeyboardBackspace} size={1} />}
            onClick={() => navigate('/admin/games')}
          >
            返回上级
          </Button>
          <Group position="right">
            <Button
              disabled={disabled}
              color="red"
              leftIcon={<Icon path={mdiDeleteOutline} size={1} />}
              variant="outline"
              onClick={() =>
                modals.openConfirmModal({
                  title: `删除比赛`,
                  children: <Text size="sm">你确定要删除比赛 "{game?.title}" 吗？</Text>,
                  onConfirm: () => onConfirmDelete(),
                  labels: { confirm: '确认', cancel: '取消' },
                  confirmProps: { color: 'red' },
                })
              }
            >
              删除比赛
            </Button>
            <Button
              leftIcon={<Icon path={mdiContentSaveOutline} size={1} />}
              disabled={disabled}
              onClick={onUpdateInfo}
            >
              保存更改
            </Button>
          </Group>
        </>
      }
    >
      <SimpleGrid cols={4}>
        <TextInput
          label="比赛标题"
          description="过长会影响显示效果"
          disabled={disabled}
          value={game?.title}
          required
          onChange={(e) => game && setGame({ ...game, title: e.target.value })}
        />
        <NumberInput
          label="队伍人数限制"
          description="0 表示不限制队伍人数"
          disabled={disabled}
          min={0}
          required
          value={game?.teamMemberCountLimit}
          onChange={(e) => game && setGame({ ...game, teamMemberCountLimit: Number(e) })}
        />
        <NumberInput
          label="队伍容器数量上限"
          description="队伍共享的容器数量 (0 表示不限制)"
          disabled={disabled}
          min={0}
          required
          value={game?.containerCountLimit}
          onChange={(e) => game && setGame({ ...game, containerCountLimit: Number(e) })}
        />
        <PasswordInput
          value={game?.publicKey || ''}
          label="比赛签名公钥"
          description="用于校验队伍 Token"
          readOnly
          onClick={() => {
            clipboard.copy(game?.publicKey || '')
            showNotification({
              color: 'teal',
              message: '公钥已复制到剪贴板',
              icon: <Icon path={mdiCheck} size={1} />,
            })
          }}
          styles={{
            innerInput: {
              cursor: 'copy',
            },
          }}
        />
        <DatePickerInput
          label="开始日期"
          placeholder="Start Date"
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
          label="开始时间"
          disabled={disabled}
          placeholder="Start Time"
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
          label="结束日期"
          disabled={disabled}
          minDate={start.toDate()}
          placeholder="End time"
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
          label="结束时间"
          disabled={disabled}
          placeholder="End time"
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
            label="比赛简介"
            description="将会显示在比赛列表中"
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
          <Stack spacing="xs">
            <TextInput
              label="邀请码"
              description="留空则不启用邀请码报名"
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
            <Switch
              disabled={disabled}
              checked={game?.acceptWithoutReview ?? false}
              label={SwitchLabel('队伍报名免审核', '队伍报名后直接设置为 Accept 状态')}
              onChange={(e) => game && setGame({ ...game, acceptWithoutReview: e.target.checked })}
            />
          </Stack>
        </Grid.Col>
        <Grid.Col span={3}>
          <Stack spacing="xs">
            <NumberInput
              label="Writeup 提交时限"
              description="比赛结束后允许提交 Writeup 的小时数"
              disabled={disabled}
              min={0}
              required
              value={wpddl}
              onChange={(e) => setWpddl(Number(e))}
            />
            <Switch
              disabled={disabled}
              checked={game?.practiceMode ?? true}
              label={SwitchLabel('练习模式', '比赛结束后仍然可以查看题目和提交')}
              onChange={(e) => game && setGame({ ...game, practiceMode: e.target.checked })}
            />
          </Stack>
        </Grid.Col>
      </Grid>
      <Group grow position="apart">
        <Textarea
          label={
            <Group spacing="sm">
              <Text size="sm">Writeup 附加说明</Text>
              <Text size="xs" c="dimmed">
                支持 Markdown 语法
              </Text>
            </Group>
          }
          value={game?.wpNote}
          w="100%"
          autosize
          disabled={disabled}
          minRows={3}
          maxRows={3}
          onChange={(e) => game && setGame({ ...game, wpNote: e.target.value })}
        />
        <MultiSelect
          label={
            <Group spacing="sm">
              <Text size="sm">参赛可选组织列表</Text>
              <Text size="xs" c="dimmed">
                添加参赛组织以开启分组榜单
              </Text>
            </Group>
          }
          searchable
          creatable
          disabled={disabled}
          placeholder="无指定可选参赛组织，将允许无组织队伍参赛"
          maxDropdownHeight={300}
          value={game?.organizations ?? []}
          styles={{
            input: {
              minHeight: 88,
              maxHeight: 88,
            },
          }}
          onChange={(e) => game && setGame({ ...game, organizations: e })}
          data={organizations.map((o) => ({ value: o, label: o })) || []}
          getCreateLabel={(query) => `+ 添加组织 "${query}"`}
          onCreate={(query) => {
            const item = { value: query, label: query }
            setOrganizations([...organizations, query])
            return item
          }}
        />
      </Group>
      <Grid grow>
        <Grid.Col span={8}>
          <Textarea
            label={
              <Group spacing="sm">
                <Text size="sm">比赛详情</Text>
                <Text size="xs" c="dimmed">
                  支持 Markdown 语法
                </Text>
              </Group>
            }
            value={game?.content}
            w="100%"
            autosize
            disabled={disabled}
            minRows={8}
            maxRows={8}
            onChange={(e) => game && setGame({ ...game, content: e.target.value })}
          />
        </Grid.Col>
        <Grid.Col span={4}>
          <Input.Wrapper label="比赛海报">
            <Dropzone
              onDrop={(files) => onUpdatePoster(files[0])}
              onReject={() => {
                showNotification({
                  color: 'red',
                  title: '文件获取失败',
                  message: '请检查文件格式和大小',
                  icon: <Icon path={mdiClose} size={1} />,
                })
              }}
              maxSize={3 * 1024 * 1024}
              accept={ACCEPT_IMAGE_MIME_TYPE}
              disabled={disabled}
              styles={{
                root: {
                  height: '198px',
                  padding: game?.poster ? '0' : '16px',
                },
              }}
            >
              <Center style={{ pointerEvents: 'none' }}>
                {game?.poster ? (
                  <Image height="195px" fit="contain" src={game.poster} alt="poster" />
                ) : (
                  <Center h="160px">
                    <Stack spacing={0}>
                      <Text size="xl" inline>
                        拖放图片或点击此处以选择海报
                      </Text>
                      <Text size="sm" c="dimmed" inline mt={7}>
                        请选择小于 3MB 的图片
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
