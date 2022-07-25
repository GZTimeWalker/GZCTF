import { FC } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Button, Accordion } from '@mantine/core'
import { mdiBackburger } from '@mdi/js'
import { Icon } from '@mdi/react'
import api from '../../../../../Api'
import WithGameTab from '../../../../../components/admin/WithGameTab'
import ChallengeInfoPart from '../../../../../components/admin/ChallengeInfoPart'

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
      headProps={{ position: 'left' }}
      head={
        <Button
          leftIcon={<Icon path={mdiBackburger} size={1} />}
          onClick={() => navigate(`/admin/games/${id}/challenges`)}
        >
          返回上级
        </Button>
      }
    >
      <Accordion chevronSize={20} defaultValue="Info">
        <Accordion.Item value="Info">
          <Accordion.Control>
            基本信息
          </Accordion.Control>
          <Accordion.Panel>
            <ChallengeInfoPart />
          </Accordion.Panel>
        </Accordion.Item>
        <Accordion.Item value="Attachments">
        <Accordion.Control>
          附件信息
        </Accordion.Control>
          <Accordion.Panel>
            <ChallengeInfoPart />
          </Accordion.Panel>
        </Accordion.Item>
        <Accordion.Item value="Container">
        <Accordion.Control>
          容器信息
        </Accordion.Control>
          <Accordion.Panel>
            <ChallengeInfoPart />
          </Accordion.Panel>
        </Accordion.Item>
      </Accordion>
    </WithGameTab>
  )
}

export default GameChallengeEdit
