import { FC, useState } from 'react'
import { useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  Text,
  Center,
  Group,
  Loader,
  Stack,
  Textarea,
  TextInput,
  NumberInput,
  Grid,
  Image,
  Button,
  InputWrapper,
} from '@mantine/core'
import { DatePicker, TimeInput } from '@mantine/dates'
import { Dropzone, IMAGE_MIME_TYPE } from '@mantine/dropzone'
import { useInputState } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import api, { GameInfoModel } from '../../../Api'

const GameInfo: FC = () => {
  const { id } = useParams()
  const navigate = useNavigate()
  const [game, setGame] = useState<GameInfoModel>()

  const [disabled, setDisabled] = useState(false)
  const [start, setStart] = useInputState(new Date())
  const [end, setEnd] = useInputState(new Date())

  useEffect(() => {
    const numId = parseInt(id ?? '-1')

    if (numId < 0) {
      showNotification({
        color: 'red',
        message: `比赛 Id 错误：${id}`,
        icon: <Icon path={mdiClose} size={1} />,
      })
      navigate('/admin/games')
      return
    }

    api.edit
      .editGetGame(numId)
      .then((data) => {
        setGame(data.data)
        setStart(new Date(data.data.start))
        setEnd(new Date(data.data.end))
      })
      .catch((err) => {
        if (err.status === 404) {
          showNotification({
            color: 'red',
            message: `比赛未找到：${id}`,
            icon: <Icon path={mdiClose} size={1} />,
          })
          navigate('/admin/games')
        }

        showNotification({
          color: 'red',
          title: '遇到了问题',
          message: `${err.error.title}`,
          icon: <Icon path={mdiClose} size={1} />,
        })
      })
  }, [id])

  const onUpdatePoster = (file: File | undefined) => {
    if (game && file) {
      api.edit
        .editUpdateGamePoster(game.id!, { file })
        .then((res) => {
          showNotification({
            color: 'teal',
            message: '修改头像成功',
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          })
          setGame({ ...game, poster: res.data })
        })
        .catch((err) => {
          showNotification({
            color: 'red',
            title: '遇到了问题',
            message: `${err.error.title}`,
            icon: <Icon path={mdiClose} size={1} />,
          })
        })
    }
  }

  const onUpdateInfo = () => {
    if (game) {
      setDisabled(true)
      api.edit
        .editUpdateGame(game.id!, game)
        .then(() => {
          showNotification({
            color: 'teal',
            message: '比赛信息已更新',
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          })
          api.game.mutateGameGamesAll()
        })
        .catch((err) => {
          showNotification({
            color: 'red',
            title: '遇到了问题',
            message: `${err.error.title}`,
            icon: <Icon path={mdiClose} size={1} />,
          })
        })
        .finally(() => {
          setDisabled(false)
        })
    }
  }

  if (!game) {
    return (
      <Center>
        <Loader />
      </Center>
    )
  }

  return (
    <Stack>
      <Grid grow>
        <Grid.Col span={8}>
          <TextInput
            label="比赛标题"
            disabled={disabled}
            value={game.title}
            onChange={(e) => setGame({ ...game, title: e.target.value })}
          />
        </Grid.Col>
        <Grid.Col span={4}>
          <NumberInput
            label="报名队伍人数限制"
            disabled={disabled}
            min={0}
            value={game.teamMemberCountLimit}
            onChange={(e) => setGame({ ...game, teamMemberCountLimit: e })}
          />
        </Grid.Col>
      </Grid>
      <Group grow position="apart">
        <DatePicker
          label="开始日期"
          disabled={disabled}
          placeholder="Start Date"
          value={start}
          clearable={false}
          onChange={(e) => setStart(e)}
          required
        />
        <TimeInput
          label="开始时间"
          disabled={disabled}
          placeholder="Start Time"
          value={start}
          onChange={(e) => setStart(e)}
          withSeconds
          required
        />
        <DatePicker
          label="结束日期"
          disabled={disabled}
          minDate={start}
          placeholder="End time"
          value={end}
          clearable={false}
          onChange={(e) => setEnd(e)}
          error={end < start}
          required
        />
        <TimeInput
          label="结束时间"
          disabled={disabled}
          placeholder="End time"
          value={end}
          onChange={(e) => setEnd(e)}
          error={end < start}
          withSeconds
          required
        />
      </Group>
      <Grid grow>
        <Grid.Col span={8}>
          <Textarea
            label="比赛简介"
            value={game.summary}
            style={{ width: '100%' }}
            autosize
            disabled={disabled}
            minRows={4}
            maxRows={4}
            onChange={(e) => setGame({ ...game, summary: e.target.value })}
          />
        </Grid.Col>
        <Grid.Col span={4}>
          <InputWrapper label="比赛海报">
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
              accept={IMAGE_MIME_TYPE}
              disabled={disabled}
              styles={{
                root: {
                  height: '7rem',
                  padding: game.poster ? '0' : '16px',
                },
              }}
            >
              {() => (
                <Center style={{ pointerEvents: 'none', height: '100%' }}>
                  {game.poster ? (
                    <Image height="6.5rem" fit="contain" src={game.poster} />
                  ) : (
                    <Stack spacing={0}>
                      <Text size="xl" inline>
                        拖放图片或点击此处以选择海报
                      </Text>
                      <Text size="sm" color="dimmed" inline mt={7}>
                        请选择小于 3MB 的图片
                      </Text>
                    </Stack>
                  )}
                </Center>
              )}
            </Dropzone>
          </InputWrapper>
        </Grid.Col>
      </Grid>
      <Textarea
        label={
          <Group spacing="sm">
            <Text>比赛详情</Text>
            <Text size="xs" color="gray">
              支持 markdown 语法
            </Text>
          </Group>
        }
        value={game.content}
        style={{ width: '100%' }}
        autosize
        disabled={disabled}
        minRows={6}
        maxRows={8}
        onChange={(e) => setGame({ ...game, content: e.target.value })}
      />
      <Group position="right">
        <Button disabled={disabled} onClick={onUpdateInfo}>
          保存更改
        </Button>
      </Group>
    </Stack>
  )
}

export default GameInfo
