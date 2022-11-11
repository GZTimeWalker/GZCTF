import React, { FC } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Button, Group, ScrollArea } from '@mantine/core'
import { mdiKeyboardBackspace, mdiPlus } from '@mdi/js'
import { Icon } from '@mdi/react'
import WithGameTab from '@Components/admin/WithGameEditTab'
import api from '@Api'

const GameWriteups: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
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
            <Button leftIcon={<Icon path={mdiPlus} size={1} />}>新建通知</Button>
          </Group>
        </>
      }
    >
      <ScrollArea
        style={{ height: 'calc(100vh-180px)', position: 'relative' }}
        offsetScrollbars
      ></ScrollArea>
    </WithGameTab>
  )
}

export default GameWriteups
