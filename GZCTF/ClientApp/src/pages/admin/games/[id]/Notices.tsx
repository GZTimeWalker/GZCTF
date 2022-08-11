import React, { FC, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Stack, Button, Text, Group, ScrollArea, Center, Title } from '@mantine/core'
import { useModals } from '@mantine/modals'
import { showNotification } from '@mantine/notifications'
import { mdiKeyboardBackspace, mdiCheck, mdiPlus } from '@mdi/js'
import { Icon } from '@mdi/react'
import GameNoticeEditCard from '@Components/GameNoticeEditCard'
import GameNoticeEditModal from '@Components/GameNoticeEditModal'
import WithGameTab from '@Components/admin/WithGameEditTab'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import api, { GameNotice } from '@Api'

const GameNoticeEdit: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const { data: gameNotices, mutate } = api.edit.useEditGetGameNotices(numId, {
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  })

  const [isEditModalOpen, setIsEditModalOpen] = useState(false)
  const [activeGameNotice, setActiveGameNotice] = useState<GameNotice | null>(null)

  // delete
  const modals = useModals()
  const onDeleteGameNotice = (gameNotice: GameNotice) => {
    modals.openConfirmModal({
      title: '删除通知',
      children: <Text> 你确定要删除通知该通知吗？</Text>,
      onConfirm: () => onConfirmDelete(gameNotice),
      centered: true,
      labels: { confirm: '删除通知', cancel: '取消' },
      confirmProps: { color: 'red' },
    })
  }
  const onConfirmDelete = (gameNotice: GameNotice) => {
    api.edit
      .editDeleteGameNotice(numId, gameNotice.id!)
      .then(() => {
        showNotification({
          color: 'teal',
          message: '通知已删除',
          icon: <Icon path={mdiCheck} size={1} />,
          disallowClose: true,
        })
        mutate(gameNotices?.filter((t) => t.id !== gameNotice.id) ?? [])
      })
      .catch(showErrorNotification)
  }

  const navigate = useNavigate()
  return (
    <WithGameTab
      headProps={{ position: 'apart' }}
      head={
        <>
          <Button
            leftIcon={<Icon path={mdiKeyboardBackspace} size={1} />}
            onClick={() => navigate('/admin/games')}
          >
            返回上级
          </Button>

          <Group position="center">
            <Button
              leftIcon={<Icon path={mdiPlus} size={1} />}
              onClick={() => {
                setActiveGameNotice(null)
                setIsEditModalOpen(true)
              }}
            >
              新建通知
            </Button>
          </Group>
        </>
      }
    >
      <ScrollArea style={{ height: 'calc(100vh-180px)', position: 'relative' }} offsetScrollbars>
        {!gameNotices || gameNotices?.length === 0 ? (
          <Center style={{ height: 'calc(100vh - 180px)' }}>
            <Stack spacing={0}>
              <Title order={2}>Ouch! 这个比赛还没有通知</Title>
              <Text>安然无事真好！</Text>
            </Stack>
          </Center>
        ) : (
          <Stack
            spacing="lg"
            align="center"
            style={{
              margin: '2%',
            }}
          >
            {gameNotices.map((gameNotice) => (
              <GameNoticeEditCard
                key={gameNotice.id}
                gameNotice={gameNotice}
                onDelete={() => {
                  onDeleteGameNotice(gameNotice)
                }}
                onEdit={() => {
                  setActiveGameNotice(gameNotice)
                  setIsEditModalOpen(true)
                }}
                style={{ width: '90%' }}
              />
            ))}
          </Stack>
        )}
      </ScrollArea>
      <GameNoticeEditModal
        centered
        size="30%"
        title={activeGameNotice ? '编辑通知' : '新建通知'}
        opened={isEditModalOpen}
        onClose={() => setIsEditModalOpen(false)}
        gameNotice={activeGameNotice}
        mutateGameNotice={(gameNotice: GameNotice) => {
          mutate([gameNotice, ...(gameNotices?.filter((n) => n.id !== gameNotice.id) ?? [])])
        }}
      />
    </WithGameTab>
  )
}

export default GameNoticeEdit
