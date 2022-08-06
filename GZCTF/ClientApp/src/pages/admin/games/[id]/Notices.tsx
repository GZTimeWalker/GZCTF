import { FC } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button } from '@mantine/core'
import { mdiBackburger } from '@mdi/js'
import { Icon } from '@mdi/react'
import WithGameEditTab from '@Components/admin/WithGameEditTab'

const GameNoticeEdit: FC = () => {
  const navigate = useNavigate()
  return (
    <WithGameEditTab
      headProps={{ position: 'left' }}
      head={
        <Button
          leftIcon={<Icon path={mdiBackburger} size={1} />}
          onClick={() => navigate('/admin/games')}
        >
          返回上级
        </Button>
      }
    ></WithGameEditTab>
  )
}

export default GameNoticeEdit
